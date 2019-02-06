using Steamworks;
using UnityEngine.Networking;
using System.Net;
using System.Collections.Generic;
using System;
using BeatSaberOnline.Controllers;
using System.Text;
using BeatSaberOnline.Views.Menus;
using System.Linq;
using UnityEngine;
using BeatSaberOnline.Utils;
using BeatSaberOnline.Workers;

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

        public static string PACKET_VERSION = "1.0.3.1";

        static string userName;
        static ulong userID;
        private static CallResult<LobbyMatchList_t> OnLobbyMatchListCallResult;
        private static CallResult<LobbyCreated_t> OnLobbyCreatedCallResult;

        private static SteamCallbacks callbacks;
        static LobbyPacket _lobbyInfo = new LobbyPacket();
        public static ConnectionState Connection { get; set; } = ConnectionState.UNDEFINED;
        public static Dictionary<ulong, LobbyPacket> LobbyData { get; set; } = new Dictionary<ulong, LobbyPacket>();

        public static void Init()
        {
            UpdateUserInfo();
            OnLobbyMatchListCallResult = CallResult<LobbyMatchList_t>.Create(OnLobbyMatchList);
            OnLobbyCreatedCallResult = CallResult<LobbyCreated_t>.Create(OnLobbyCreated);
            callbacks = new SteamCallbacks();
            _lobbyInfo = new LobbyPacket();


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
                    Logger.Debug($"Game was started with +connect_lobby, lets join it @ {lobbyId}");
                    JoinLobby(new CSteamID(lobbyId));
                }
            }
        }

        public static void UpdateUserInfo()
        {
            if (userID == 0 || userName == null)
            {
                Logger.Debug($"Updating current user info");
                userName = SteamFriends.GetPersonaName();
                userID = SteamUser.GetSteamID().m_SteamID;
            }
        }

        public static LobbyPacket GetLobbyData()
        {
            return _lobbyInfo;
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
            return Connection;
        }

        public static void SetConnectionState(ConnectionState _connection)
        {
            Connection = _connection;
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
            SendLobbyPacket(true);
        }
        public static void SetReady()
        {
            Logger.Debug($"Broadcast to our lobby that we are ready");
            Controllers.PlayerController.Instance._playerInfo.Ready = true;
            if (_lobbyInfo.UsedSlots == 1)
            {
                StartPlaying();
            }
            WaitingMenu.RefreshData();
        }

        public static GameplayModifiers GetGameplayModifiers()
        {
            return _lobbyInfo.GameplayModifiers;
        }
        public static void ClearPlayerReady(CSteamID steamId, bool push)
        {
            if (push) {
                Logger.Debug($"Broadcast to our lobby that our ready status should be cleared");
                Controllers.PlayerController.Instance._playerInfo.Ready = false;
            }
        }

        public static void StartPlaying()
        {
            _lobbyInfo.Screen = LobbyPacket.SCREEN_TYPE.PLAY_SONG;
            SendLobbyPacket(true);
        }
        public static void StartGame()
        {
            _lobbyInfo.Screen = LobbyPacket.SCREEN_TYPE.IN_GAME;
            SendLobbyPacket(true);
        }
        
        public static void RequestPlay(GameplayModifiers gameplayModifiers)
        {
            if (IsHost())
            {
                try
                {
                    Logger.Debug($"update the current screen to the waiting screen while people download the song");
                    LevelSO song = SongListUtils.GetInstalledSong();
                    if (song != null)
                    {
                        setLobbyStatus("Loading " + song.songName + " by " + song.songAuthorName);
                    }
                    _lobbyInfo.GameplayModifiers = gameplayModifiers;
                    _lobbyInfo.Screen = LobbyPacket.SCREEN_TYPE.WAITING;
                    SendLobbyPacket(true);
                } catch (Exception e)
                {
                    Logger.Error(e);
                }
            }
        }

        public static string GetSongId()
        {
            return _lobbyInfo.CurrentSongId;
        }
        public static string GetSongName()
        {
            return _lobbyInfo.CurrentSongName;
        }

        public static byte GetSongDifficulty()
        {
            return _lobbyInfo.CurrentSongDifficulty;
        }
        public static float GetSongOffset()
        {
            return _lobbyInfo.CurrentSongOffset;
        }
        public static void SetSong(string songId, string songName)
        {
            _lobbyInfo.CurrentSongId = songId;
            _lobbyInfo.CurrentSongName = songName;
            Logger.Debug($"We want to play {songId} - {songName}");
            SendLobbyPacket(true);
        }
         public static void SetSongOffset(float offset)
        {
            _lobbyInfo.CurrentSongOffset = offset;
            SendLobbyPacket(true);
        }
        public static bool IsHost()
        {
            if (_lobbyInfo.LobbyID.m_SteamID == 0 || SteamMatchmaking.GetNumLobbyMembers(_lobbyInfo.LobbyID) == 1) { return true;  }
            return SteamMatchmaking.GetLobbyOwner(_lobbyInfo.LobbyID).m_SteamID == GetUserID();
        }
        public static ulong GetHostId()
        {
            return SteamMatchmaking.GetLobbyOwner(_lobbyInfo.LobbyID).m_SteamID;
        }

        public static void SetDifficulty(byte songDifficulty)
        {
            Logger.Debug($"We want to play on {songDifficulty}");
            _lobbyInfo.CurrentSongDifficulty = songDifficulty;
            SendLobbyPacket(true);
        }

        public static void StopSong()
        {
            Logger.Debug($"Broadcast to the lobby that we are back on the menu");

            _lobbyInfo.Screen = LobbyPacket.SCREEN_TYPE.MENU;
            SendLobbyPacket(true);
            setLobbyStatus("Waiting In Menu");
        }

        public static void ResetScreen()
        {
                Logger.Debug($"Clear the current screen from the lobby");

                _lobbyInfo.Screen = LobbyPacket.SCREEN_TYPE.NONE;
                SendLobbyPacket(true);
        }

        public static int getUserCount()
        {
            return SteamMatchmaking.GetNumLobbyMembers(_lobbyInfo.LobbyID) + 1;
        }
        public static void FinishSong()
        {
            Logger.Debug($"We have finished the song");
            setLobbyStatus("Waiting In Menu");
        }

        private static void SendLobbyPacket(bool reqHost = false)
        {
             if (reqHost && !IsHost()) return;
             Logger.Debug($"Sending {_lobbyInfo.ToString()}");
             SteamMatchmaking.SetLobbyData(_lobbyInfo.LobbyID, "LOBBY_INFO", _lobbyInfo.Serialize());
        }

        public static CGameID GetGameID()
        {
            var fgi = new FriendGameInfo_t();
            SteamFriends.GetFriendGamePlayed(new CSteamID(userID), out fgi);
            return fgi.m_gameID;
        }

        public static void RequestLobbies()
        {
            if (!SteamManager.Initialized)
            {
                Logger.Error("CONNECTION FAILED");
                return;
            }
            Logger.Debug($"Requesting list of all lobbies from steam");

            LobbyData.Clear();
            MultiplayerListing.refreshLobbyList();

            SteamMatchmaking.AddRequestLobbyListFilterSlotsAvailable(1);
            SteamMatchmaking.AddRequestLobbyListStringFilter("version", PACKET_VERSION, ELobbyComparison.k_ELobbyComparisonEqual);
            SteamAPICall_t apiCall = SteamMatchmaking.RequestLobbyList();
            OnLobbyMatchListCallResult.Set(apiCall);

            int cFriends = SteamFriends.GetFriendCount(EFriendFlags.k_EFriendFlagImmediate);
            for (int i = 0; i < cFriends; i++)
            {
                FriendGameInfo_t friendGameInfo;
                CSteamID steamIDFriend = SteamFriends.GetFriendByIndex(i, EFriendFlags.k_EFriendFlagImmediate); SteamFriends.GetFriendGamePlayed(steamIDFriend, out friendGameInfo);
                if (friendGameInfo.m_gameID == GetGameID() && friendGameInfo.m_steamIDLobby.IsValid())
                {
                    SteamMatchmaking.RequestLobbyData(friendGameInfo.m_steamIDLobby);
                }
            }
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
        public static void PlayerConnected()
        {
            _lobbyInfo.UsedSlots += 1;
            if (_lobbyInfo.UsedSlots > 5)
            {
                float mod = _lobbyInfo.UsedSlots / 5;
                float modifier = mod - (mod * 0.88f);
                if (modifier > 0.6)
                {
                    modifier = 0.6f;
                }
                GameController.TPS_MODIFIER = 1 - modifier;
                Controllers.PlayerController.Instance.RestartBroadcasting();
            }
            SendLobbyPacket(true);
        }
        public static void PlayerDisconnected()
        {
            _lobbyInfo.UsedSlots -= 1;
            if (_lobbyInfo.UsedSlots > 5)
            {
                float mod = _lobbyInfo.UsedSlots / 5;
                float modifier = mod - (mod * 0.88f);
                if (modifier > 0.6)
                {
                    modifier = 0.6f;
                }
                GameController.TPS_MODIFIER = 1 - modifier;
                Controllers.PlayerController.Instance.RestartBroadcasting();
            }
            if (IsHost())
            {
                _lobbyInfo.HostName = GetUserName();
                SendLobbyPacket(true);
            }
        }

        public static void OnLobbyMatchList(LobbyMatchList_t pCallback, bool bIOFailure)
        {
            if (!SteamManager.Initialized)
            {
                Logger.Error("CONNECTION FAILED");
                return;
            }
            uint numLobbies = pCallback.m_nLobbiesMatching;
            Logger.Debug($"Found {numLobbies} total lobbies");
            try
            {
                for (int i = 0; i < numLobbies; i++)
                {
                    CSteamID lobbyId = SteamMatchmaking.GetLobbyByIndex(i);
                    if (lobbyId.m_SteamID == _lobbyInfo.LobbyID.m_SteamID) { continue; }
                    LobbyPacket info = new LobbyPacket(SteamMatchmaking.GetLobbyData(lobbyId, "LOBBY_INFO"));

                    SetOtherLobbyData(lobbyId.m_SteamID, info, false);
                    Logger.Info($"{info.HostName} has {info.UsedSlots} users in it and is currently {info.Status}");
                }
            } catch (Exception e)
            {
                Logger.Error(e);
            }

            MultiplayerListing.refreshLobbyList();
        }


        public static void CreateLobby(bool privateLobby)
        {
            if (isLobbyConnected()) { return; }
            Logger.Debug($"Creating a lobby");
            SteamAPICall_t handle = SteamMatchmaking.CreateLobby(privateLobby ? ELobbyType.k_ELobbyTypeFriendsOnly : ELobbyType.k_ELobbyTypePublic, Config.Instance.MaxLobbySize);
            OnLobbyCreatedCallResult.Set(handle);
        }

        public static void InviteUserToLobby(CSteamID userId)
        {
            if (!SteamManager.Initialized)
            {
                Logger.Error("CONNECTION FAILED");
                return;
            }
            Logger.Debug($"Inviting {userId} to our lobby");

            bool ret = SteamMatchmaking.InviteUserToLobby(_lobbyInfo.LobbyID, userId);
        }

        public static bool isLobbyConnected()
        {
            return SteamManager.Initialized && Connection == ConnectionState.CONNECTED && _lobbyInfo.LobbyID.m_SteamID > 0;
        }
        private static void OnLobbyCreated(LobbyCreated_t pCallback, bool bIOFailure)
        {

            if (!SteamManager.Initialized)
            {
                Logger.Error("CONNECTION FAILED");
                return;
            }
            if (!bIOFailure) {
                Scoreboard.Instance.UpsertScoreboardEntry(Controllers.PlayerController.Instance._playerInfo.playerId, Controllers.PlayerController.Instance._playerInfo.playerName);
                _lobbyInfo.LobbyID = new CSteamID(pCallback.m_ulSteamIDLobby);
                _lobbyInfo.TotalSlots = SteamMatchmaking.GetLobbyMemberLimit(_lobbyInfo.LobbyID);
                _lobbyInfo.HostName = GetUserName();
                Logger.Debug($"Lobby has been created");
                var hostUserId = SteamMatchmaking.GetLobbyOwner(_lobbyInfo.LobbyID);
                SteamMatchmaking.SetLobbyData(_lobbyInfo.LobbyID, "version", PACKET_VERSION);

                var me = SteamUser.GetSteamID();
                Connection = ConnectionState.CONNECTED;
                if (hostUserId.m_SteamID == me.m_SteamID)
                {
                    setLobbyStatus("Waiting In Menu");
                        
                    SendLobbyPacket(true);
                }
            }
        }
        public static void JoinLobby(CSteamID lobbyId)
        {
            if (!SteamManager.Initialized)
            {
                Connection = ConnectionState.FAILED;
                Logger.Error("CONNECTION FAILED");
                return;
            }
            if (_lobbyInfo.LobbyID.m_SteamID > 0)
            {
                Logger.Debug($"We are already in another lobby, lets disconnect first");
                Disconnect();
            }
            Connection = ConnectionState.CONNECTING;
            _lobbyInfo.LobbyID = lobbyId;
            Scoreboard.Instance.UpsertScoreboardEntry(Controllers.PlayerController.Instance._playerInfo.playerId, Controllers.PlayerController.Instance._playerInfo.playerName);

            Logger.Debug($"Joining a new steam lobby {lobbyId}");
            SteamMatchmaking.JoinLobby(lobbyId);
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

        public static void UpdateLobbyPacket(LobbyPacket info)
        {
            _lobbyInfo = info;
        }
        public static void setLobbyStatus(string value)
        {
            Logger.Debug($"Update lobby status to {value}");
            _lobbyInfo.Status = value;
            SendLobbyPacket(true);
        }
        
        public static void SendPlayerPacket(PlayerPacket playerInfo)
        {
            var message = playerInfo.Serialize().Trim();
            byte[] bytes = Encoding.UTF8.GetBytes(message);
            SendToAllInLobby(bytes);
        }

        public static void SendVoip(VoipPacket voip)
        {
            var message = voip.Serialize().Trim();
            byte[] bytes = Encoding.UTF8.GetBytes(message);
            SendToAllInLobby(bytes, 1);
        }

        public static void SendToAllInLobby(byte[] bytes, int channel = 0)
        {
            int numMembers = SteamMatchmaking.GetNumLobbyMembers(_lobbyInfo.LobbyID);
            for (int i = 0; i < numMembers; i++)
            {
                CSteamID member = SteamMatchmaking.GetLobbyMemberByIndex(_lobbyInfo.LobbyID, i);
                if (member.m_SteamID != SteamAPI.GetUserID())
                {
                    SteamNetworking.SendP2PPacket(member, bytes, (uint)bytes.Length, EP2PSend.k_EP2PSendReliable, channel);
                }
            }
        }

        public static void DisconnectPlayer(ulong playerId)
        {
            try
            {
                Logger.Debug($"{playerId} disconnected from current lobby");
                _lobbyInfo.UsedSlots -= 1;
                Scoreboard.Instance.RemoveScoreboardEntry(playerId);
                Controllers.PlayerController.Instance.RemoveConnectedPlayer(playerId);
                SendLobbyPacket(true);
            } catch (Exception e)
            {
                Logger.Error(e);
            }
        }

        public static void KickAll()
        {
            List<ulong> connectedUsers = Controllers.PlayerController.Instance.GetConnectedPlayers();
            for (int i = 0; i < connectedUsers.Count; i++)
            {
                SteamMatchmaking.SetLobbyMemberData(_lobbyInfo.LobbyID, "STATUS", $"KICK={connectedUsers[i]}");
            }
        }

        public static void Disconnect()
        {
            try
            {
                Logger.Debug($"Disconnect from current lobby");
                SteamMatchmaking.SetLobbyMemberData(_lobbyInfo.LobbyID, "STATUS", "DISCONNECTED");
                Controllers.PlayerController.Instance.StopBroadcasting();
                _lobbyInfo.HostName = "";
                SendLobbyPacket(true);                
                Connection = ConnectionState.DISCONNECTED;
                SteamMatchmaking.LeaveLobby(_lobbyInfo.LobbyID);
                Controllers.PlayerController.Instance.DestroyAvatars();
                WaitingMenu.firstInit = true;
                WaitingMenu.queuedSong = null;
                _lobbyInfo = new LobbyPacket();
                UpdateUserInfo();
                VoiceChatWorker.VoipEnabled = false;
                Scoreboard.Instance.RemoveAll();
                SongListUtils.InSong = false;
                Controllers.PlayerController.Instance._playerInfo = new PlayerPacket(GetUserName(), GetUserID());
                LobbyData.Clear();
            } catch (Exception e)
            {
                Logger.Error(e);
            }
        }
        public static void SetOtherLobbyData(ulong lobbyId, LobbyPacket info, bool refresh = true)
        {
            string version = SteamMatchmaking.GetLobbyData(new CSteamID(lobbyId), "version");

            info.UsedSlots = SteamMatchmaking.GetNumLobbyMembers(new CSteamID(lobbyId));
            if (info.UsedSlots > 0 && info.HostName != "" && version == PACKET_VERSION)
            {
                LobbyData.Add(lobbyId, info);

                MultiplayerListing.refreshLobbyList();
            }
        }
    }
}
