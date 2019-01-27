using Steamworks;
using UnityEngine.Networking;
using System.Net;
using System.Collections.Generic;
using System;
using BeatSaberOnline.Controllers;
using System.Text;
using BeatSaberOnline.Views.Menus;
using System.Linq;

namespace BeatSaberOnline.Data.Steam
{
    public static class SteamAPI
    {

        public enum ConnectionState
        {
            UNDEFINED,
            CONNECTING,
            CANCELLED,
            CONNECTED,
            FAILED,
            DISCONNECTING,
            DISCONNECTED
        }

        public const string GAME_ID = "beat-saber-online-multiplayer";

        static string userName;
        static ulong userID;
        private static CallResult<LobbyMatchList_t> OnLobbyMatchListCallResult;
        private static CallResult<LobbyCreated_t> OnLobbyCreatedCallResult;

        private static SteamCallbacks callbacks;
        static LobbyInfo _lobbyInfo;


        public static void Init()
        {
            UpdateUserInfo();
            OnLobbyMatchListCallResult = CallResult<LobbyMatchList_t>.Create(OnLobbyMatchList);
            OnLobbyCreatedCallResult = CallResult<LobbyCreated_t>.Create(OnLobbyCreated);
            callbacks = new SteamCallbacks();
            _lobbyInfo = new LobbyInfo();


            string[] args = System.Environment.GetCommandLineArgs();
            string input = "";
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "+connect_lobby" && args.Length > i + 1)
                {
                    input = args[i + 1];
                }
            }

            if (!string.IsNullOrEmpty(input))
            {
                ulong lobbyId = Convert.ToUInt64(input);

                if (lobbyId > 0)
                {
                    JoinLobby(new CSteamID(lobbyId));
                }
            }
        }

        public static void UpdateUserInfo()
        {
            if (userID == 0 || userName == null)
            {
                userName = SteamFriends.GetPersonaName();
                userID = SteamUser.GetSteamID().m_SteamID;
            }
        }

        public static string GetUserName()
        {
            return userName;
        }

        public static ulong GetUserID()
        {
            return userID;
        }

        public static CSteamID getLobbyID()
        {
            return _lobbyInfo.LobbyID;
        }

        public static ConnectionState GetConnectionState()
        {
            return _lobbyInfo.Connection;
        }

        public static void SetConnectionState(ConnectionState Connection)
        {
            _lobbyInfo.Connection = Connection;
        }


        public static bool IsLobbyJoinable()
        {
            return _lobbyInfo.Joinable;
        }

        public static int getSlotsOpen()
        {
            return _lobbyInfo.TotalSlots;
        }

        public static void ToggleLobbyJoinable()
        {
            _lobbyInfo.Joinable = !_lobbyInfo.Joinable;
            SteamMatchmaking.SetLobbyJoinable(_lobbyInfo.LobbyID, _lobbyInfo.Joinable);
        }
        public static void SetReady()
        {
            if (!_lobbyInfo.ReadyState.ContainsKey(new CSteamID(GetUserID())))
            {
                SteamMatchmaking.SetLobbyMemberData(_lobbyInfo.LobbyID, "ReadyStatus", "Ready");
                SetPlayerReady(new CSteamID(GetUserID()));
            }
            WaitingMenu.RefreshData(false);
        }

        public static void ClearPlayerReady(CSteamID steamId, bool push)
        {
            _lobbyInfo.ReadyState.Remove(steamId);
            if (push) { SteamMatchmaking.SetLobbyMemberData(_lobbyInfo.LobbyID, "ReadyStatus", ""); }
            if (!IsHost()) return;
            Dictionary<string, bool> status = SteamAPI.getAllPlayerStatusesInLobby();
            bool allReady = status.All(u => !u.Value);
            if (allReady)
            {
                SteamMatchmaking.SetLobbyData(_lobbyInfo.LobbyID, "Screen", "IN_GAME");
            }

        }
        public static void SetPlayerReady(CSteamID steamId)
        {
            _lobbyInfo.ReadyState.Add(steamId, true);
            WaitingMenu.RefreshData(false); 
            if (!IsHost()) return;  
            Dictionary<string, bool> status = SteamAPI.getAllPlayerStatusesInLobby();
            bool allReady = status.All(u => u.Value);

            if (allReady)
            {
                SteamMatchmaking.SetLobbyData(_lobbyInfo.LobbyID, "Screen", "PLAY_SONG");
            }
        }

        public static Dictionary<string, bool> getAllPlayerStatusesInLobby()
        {
            Dictionary<string, bool> status = new Dictionary<string, bool>();
            int numMembers = SteamMatchmaking.GetNumLobbyMembers(_lobbyInfo.LobbyID);
            for (int i = 0; i < numMembers; i++)
            {
                CSteamID member = SteamMatchmaking.GetLobbyMemberByIndex(_lobbyInfo.LobbyID, i);
                string name =  SteamFriends.GetFriendPersonaName(member);
                status.Add(name, _lobbyInfo.ReadyState.ContainsKey(member) && _lobbyInfo.ReadyState[member]);
            }
            return status;
        }
        public static void RequestPlay()
        {
            if (IsHost())
            {
                LevelSO song = WaitingMenu.GetInstalledSong();
                if (song != null)
                {
                    setLobbyStatus("Loading " + song.songName + " by " + song.songAuthorName);
                }
                SteamMatchmaking.SetLobbyData(_lobbyInfo.LobbyID, "Screen", "WAITING");
            }
        }
        public static void UpdateSongData(string songId, byte songDifficulty)
        {
            _lobbyInfo.SongId = songId;
            _lobbyInfo.SongDifficulty = songDifficulty;
        }

        public static string GetSongId()
        {
            return _lobbyInfo.SongId;
        }
        public static byte GetSongDifficulty()
        {
            return _lobbyInfo.SongDifficulty;
        }

        public static void SetSong(string songId)
        {
            _lobbyInfo.SongId = songId;
            if (IsHost())
            {
                SteamMatchmaking.SetLobbyData(_lobbyInfo.LobbyID, "SongId", songId);
            }
        }

        public static bool IsHost()
        {
            return SteamMatchmaking.GetLobbyOwner(_lobbyInfo.LobbyID).m_SteamID == GetUserID();
        }

        public static void SetDifficulty(byte songDifficulty)
        {
            _lobbyInfo.SongDifficulty = songDifficulty;
            if (IsHost())
            {
                SteamMatchmaking.SetLobbyData(_lobbyInfo.LobbyID, "SongDifficulty", Encoding.ASCII.GetString(new[] { songDifficulty }));
            }
        }

        public static void StopSong()
        {
            SteamMatchmaking.SetLobbyData(_lobbyInfo.LobbyID, "Screen", "MENU");
            setLobbyStatus("Waiting In Menu");
        }

        public static void ResetScreen()
        {
            if (IsHost())
            {
                SteamMatchmaking.SetLobbyData(_lobbyInfo.LobbyID, "Screen", "");
            }
        }

        public static int getUserCount()
        {
            return SteamMatchmaking.GetNumLobbyMembers(_lobbyInfo.LobbyID) + 1;
        }
        public static void FinishSong()
        {
            _lobbyInfo.ReadyState.Clear();
            _lobbyInfo.SongDifficulty = 0;
            _lobbyInfo.SongId = null;
            SteamMatchmaking.SetLobbyMemberData(_lobbyInfo.LobbyID, "ReadyStatus", "");
            setLobbyStatus("Waiting In Menu");

        }
        public static void IncreaseSlots()
        {
            _lobbyInfo.TotalSlots += 1;
            if (_lobbyInfo.TotalSlots > _lobbyInfo.MaxSlots)
            {
                _lobbyInfo.TotalSlots = 2;
            }
            SteamMatchmaking.SetLobbyMemberLimit(_lobbyInfo.LobbyID, _lobbyInfo.TotalSlots);
        }

        public static CGameID GetGameID()
        {
            var fgi = new FriendGameInfo_t();
            SteamFriends.GetFriendGamePlayed(new CSteamID(userID), out fgi);
            return fgi.m_gameID;
        }

        public static void GetServers()
        {
            if (!SteamManager.Initialized)
            {
                Logger.Error("CONNECTION FAILED");
                return;
            }
            SteamAPICall_t apiCall = SteamMatchmaking.RequestLobbyList();
            OnLobbyMatchListCallResult.Set(apiCall);
        }

        public static Dictionary<CSteamID, string[]> GetOnlineFriends()
        {
            var friends = new Dictionary<CSteamID, string[]>(); if (!SteamManager.Initialized)
            {
                Logger.Error("CONNECTION FAILED");
                return friends;
            }
            try
            {
                int friendCount = SteamFriends.GetFriendCount(EFriendFlags.k_EFriendFlagImmediate);
                for (int i = 0; i < friendCount; ++i)
                {
                    CSteamID friendSteamId = SteamFriends.GetFriendByIndex(i, EFriendFlags.k_EFriendFlagImmediate);
                    string friendName = SteamFriends.GetFriendPersonaName(friendSteamId);
                    EPersonaState friendState = SteamFriends.GetFriendPersonaState(friendSteamId);
                    if (friendState != EPersonaState.k_EPersonaStateOffline)
                    {
                        var fgi = new FriendGameInfo_t();
                        bool ret = SteamFriends.GetFriendGamePlayed(friendSteamId, out fgi);
                        friends.Add(friendSteamId,  new string[]{ friendName, ""+fgi.m_gameID});
                    }
                }
            } catch (Exception e)
            {
                Logger.Error(e);
            }
            return friends;
        }
        
        public static void OpenInviteScreen()
        {
            if (!SteamManager.Initialized)
            {
                Logger.Error("CONNECTION FAILED");
                return;
            }
            SteamFriends.ActivateGameOverlayInviteDialog(_lobbyInfo.LobbyID);
        }

        public static void OnLobbyMatchList(LobbyMatchList_t pCallback, bool bIOFailure)
        {
            if (!SteamManager.Initialized)
            {
                Logger.Error("CONNECTION FAILED");
                return;
            }
            uint numLobbies = pCallback.m_nLobbiesMatching;

        }

        public static void CreateLobby()
        {
            SteamAPICall_t handle = SteamMatchmaking.CreateLobby(Config.Instance.IsPublic ? ELobbyType.k_ELobbyTypePublic : ELobbyType.k_ELobbyTypeFriendsOnly, Config.Instance.MaxLobbySize);
            OnLobbyCreatedCallResult.Set(handle);
        }

        public static void InviteUserToLobby(CSteamID userId)
        {
            if (!SteamManager.Initialized)
            {
                Logger.Error("CONNECTION FAILED");
                return;
            }
            bool ret = SteamMatchmaking.InviteUserToLobby(_lobbyInfo.LobbyID, userId);
        }

        public static bool isLobbyConnected()
        {
            return SteamManager.Initialized && _lobbyInfo.Connection == ConnectionState.CONNECTED;
        }
        private static void OnLobbyCreated(LobbyCreated_t pCallback, bool bIOFailure)
        {

            if (!SteamManager.Initialized)
            {
                Logger.Error("CONNECTION FAILED");
                return;
            }

            _lobbyInfo.LobbyID = new CSteamID(pCallback.m_ulSteamIDLobby);

            var hostUserId = SteamMatchmaking.GetLobbyOwner(_lobbyInfo.LobbyID);
            var me = SteamUser.GetSteamID();
            if (bIOFailure)
            {
                Logger.Info("FAILED TO CREATE");
            }

            if (hostUserId.m_SteamID == me.m_SteamID)
            {
                setLobbyStatus("Waiting In Menu");
                SteamMatchmaking.SetLobbyData(_lobbyInfo.LobbyID, "game", GAME_ID);
            }
        }
        
        public static void JoinLobby(CSteamID lobbyId)
        {
            if (!SteamManager.Initialized)
            {
                _lobbyInfo.Connection = ConnectionState.FAILED;
                Logger.Error("CONNECTION FAILED");
                return;
            }
            if (_lobbyInfo.LobbyID.m_SteamID > 0)
            {
                Disconnect();
            }
            _lobbyInfo.Connection = ConnectionState.CONNECTING;
            _lobbyInfo.LobbyID = lobbyId;
            SteamMatchmaking.JoinLobby(lobbyId);
        }
        
        public static Dictionary<CSteamID, string> getAvailableLobbies()
        {
            Dictionary<CSteamID, string> availableLobbies = new Dictionary<CSteamID, string>();

            if (!SteamManager.Initialized)
            {
                Logger.Error("CONNECTION FAILED");
                return availableLobbies;
            }
            int cFriends = SteamFriends.GetFriendCount(EFriendFlags.k_EFriendFlagImmediate);
                for (int i = 0; i < cFriends; i++)
                {
                    FriendGameInfo_t friendGameInfo;
                    CSteamID steamIDFriend = SteamFriends.GetFriendByIndex(i, EFriendFlags.k_EFriendFlagImmediate); SteamFriends.GetFriendGamePlayed(steamIDFriend, out friendGameInfo);
                    if (friendGameInfo.m_gameID == GetGameID() && friendGameInfo.m_steamIDLobby.IsValid() && friendGameInfo.m_steamIDLobby != _lobbyInfo.LobbyID)
                    {
                        availableLobbies.Add(friendGameInfo.m_steamIDLobby, SteamFriends.GetFriendPersonaName(steamIDFriend));
                    }
                }
            return availableLobbies;
        }

        public static bool IsMemberInSteamLobby(CSteamID steamUser)
        {
            if (!SteamManager.Initialized)
            {
                Logger.Error("CONNECTION FAILED");
                return false;
            }
            int numMembers = SteamMatchmaking.GetNumLobbyMembers(_lobbyInfo.LobbyID);

                for (int i = 0; i < numMembers; i++)
                {
                    var member = SteamMatchmaking.GetLobbyMemberByIndex(_lobbyInfo.LobbyID, i);

                    if (member.m_SteamID == steamUser.m_SteamID)
                    {
                        return true;
                    }
                }

            return false;
        }

        public static Dictionary<CSteamID, string> GetMembersInLobby()
        {
            Dictionary<CSteamID, string> members = new Dictionary<CSteamID, string>();
            if (!SteamManager.Initialized)
            {
                Logger.Error("CONNECTION FAILED");
                return members;
            }
            int numMembers = SteamMatchmaking.GetNumLobbyMembers(_lobbyInfo.LobbyID);
            for (int i = 0; i < numMembers; i++)
            {
                CSteamID member = SteamMatchmaking.GetLobbyMemberByIndex(_lobbyInfo.LobbyID, i);
                members.Add(member, SteamFriends.GetFriendPersonaName(member));
            }

            return members;
        }

        public static void setLobbyStatus(string value)
        {
            SteamMatchmaking.SetLobbyData(_lobbyInfo.LobbyID, "status", value);
        }

        public static string getLobbyStatus(CSteamID lobbyID)
        {
            return SteamMatchmaking.GetLobbyData(lobbyID, "status");
        }
        
        public static void SendPlayerInfo(PlayerInfo playerInfo)
        {
            var message = playerInfo.Serialize().Trim();
            byte[] bytes = Encoding.UTF8.GetBytes(message);
            SendToAllInLobby(bytes);
        }

        public static void SendToAllInLobby(byte[] bytes)
        {
            int numMembers = SteamMatchmaking.GetNumLobbyMembers(_lobbyInfo.LobbyID);
            for (int i = 0; i < numMembers; i++)
            {
                CSteamID member = SteamMatchmaking.GetLobbyMemberByIndex(_lobbyInfo.LobbyID, i);
                if (member.m_SteamID != SteamAPI.GetUserID())
                {
                    SteamNetworking.SendP2PPacket(member, bytes, (uint)bytes.Length, EP2PSend.k_EP2PSendReliable);
                }
            }
        }
        public static void Disconnect()
        {
            SteamMatchmaking.LeaveLobby(_lobbyInfo.LobbyID);
            _lobbyInfo.LobbyID = new CSteamID(0);
            _lobbyInfo.Connection = ConnectionState.DISCONNECTED;
            Controllers.PlayerController.Instance.DestroyAvatars();
        }
    }
}
