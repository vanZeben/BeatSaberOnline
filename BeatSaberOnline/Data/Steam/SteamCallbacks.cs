using BeatSaberOnline.Controllers;
using BeatSaberOnline.Utils;
using BeatSaberOnline.Views.Menus;
using SongLoaderPlugin;
using SongLoaderPlugin.OverrideClasses;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeatSaberOnline.Data.Steam
{
    class SteamCallbacks
    {

        private Callback<GameLobbyJoinRequested_t> m_GameLobbyJoinRequested;
        private Callback<LobbyEnter_t> m_LobbyEnter_t;
        private Callback<LobbyDataUpdate_t> m_LobbyDataUpdate_t;
        protected Callback<P2PSessionRequest_t> m_P2PSessionRequest;
        protected Callback<P2PSessionConnectFail_t> m_P2PSessionConnectFail_t;
        protected Callback<LobbyChatUpdate_t> m_LobbyChatUpdate_t;

        private LobbyPacket.SCREEN_TYPE currentScreen;
        public SteamCallbacks()
        {
            m_GameLobbyJoinRequested = Callback<GameLobbyJoinRequested_t>.Create(OnGameLobbyJoinRequested);
            m_LobbyEnter_t = Callback<LobbyEnter_t>.Create(OnLobbyEnter);
            m_LobbyDataUpdate_t = Callback<LobbyDataUpdate_t>.Create(OnLobbyDataUpdate);
            m_P2PSessionRequest = Callback<P2PSessionRequest_t>.Create(OnP2PSessionRequest);
            m_P2PSessionConnectFail_t = Callback<P2PSessionConnectFail_t>.Create(OnP2PSessionFail);
            m_LobbyChatUpdate_t = Callback<LobbyChatUpdate_t>.Create(OnLobbyChatUpdate);
        }

        private void OnGameLobbyJoinRequested(GameLobbyJoinRequested_t pCallback)
        {
            Logger.Debug($"Attempting to join {pCallback.m_steamIDFriend}'s lobby @ {pCallback.m_steamIDLobby}");
            SteamAPI.JoinLobby(pCallback.m_steamIDLobby);
        }
        private bool DidScreenChange(LobbyPacket.SCREEN_TYPE newScreen, LobbyPacket.SCREEN_TYPE val)
        {
            return currentScreen != val && val == newScreen;
        }
        public void OnLobbyDataUpdate(LobbyDataUpdate_t pCallback)
        {
            if (pCallback.m_ulSteamIDLobby == pCallback.m_ulSteamIDMember)
            {
                if (pCallback.m_ulSteamIDLobby == 0) return;
                LobbyPacket info = new LobbyPacket(SteamMatchmaking.GetLobbyData(new CSteamID(pCallback.m_ulSteamIDLobby), "LOBBY_INFO"));

                Logger.Debug($"Received: {info.ToString()}");
                if (pCallback.m_ulSteamIDLobby == SteamAPI.getLobbyID().m_SteamID)
                {
                    SteamAPI.UpdateLobbyPacket(info);
                    if (DidScreenChange(info.Screen, LobbyPacket.SCREEN_TYPE.WAITING))
                    {
                        currentScreen = info.Screen;
                        Logger.Debug($"Song has been selected, going to the waiting screen");
                        WaitingMenu.Instance.Present();
                    }
                    else if (DidScreenChange(info.Screen, LobbyPacket.SCREEN_TYPE.MENU))
                    {
                        currentScreen = info.Screen;
                        Logger.Debug($"Song has finished, updating state to menu");
                        MultiplayerLobby.UpdateJoinButton();
                        GameController.Instance.SongFinished(null, null, null, null);
                    }
                    else if (DidScreenChange(info.Screen, LobbyPacket.SCREEN_TYPE.PLAY_SONG))
                    {
                        currentScreen = info.Screen;
                        Logger.Debug($"Host requested to play the current song {info.CurrentSongId}");
                        LevelSO song = SongListUtils.GetInstalledSong();
                        if (SteamAPI.IsHost())
                        {
                            SteamAPI.setLobbyStatus("Playing " + song.songName + " by " + song.songAuthorName);
                        }
                        try
                        {
                            SteamAPI.ClearPlayerReady(new CSteamID(SteamAPI.GetUserID()), true);
                            SongListUtils.StartSong(song, SteamAPI.GetSongDifficulty(), info.GameplayModifiers, null);
                        } catch(Exception e)
                        {
                            Logger.Error(e);
                        }
                    } else if (DidScreenChange(info.Screen, LobbyPacket.SCREEN_TYPE.IN_GAME))
                    {
                        MultiplayerLobby.UpdateJoinButton();
                    }
                } else
                {
                    SteamAPI.SetOtherLobbyData(pCallback.m_ulSteamIDLobby, info);
                }
            } else {
                string status = SteamMatchmaking.GetLobbyMemberData(new CSteamID(pCallback.m_ulSteamIDLobby), new CSteamID(pCallback.m_ulSteamIDMember), "STATUS");
                if (status == "DISCONNECTED")
                {
                    SteamAPI.DisconnectPlayer(pCallback.m_ulSteamIDMember);
                } else if (status.StartsWith("KICKED"))
                {
                    ulong playerId = Convert.ToUInt64(status.Substring(7));
                    if (playerId != 0 && playerId == SteamAPI.GetUserID())
                    {
                        SteamAPI.Disconnect();
                    }
                }
            }
        }

        public static void OnLobbyChatUpdate(LobbyChatUpdate_t pCallback)
        {
            switch ((uint) pCallback.m_rgfChatMemberStateChange)
            {
                case (uint)EChatMemberStateChange.k_EChatMemberStateChangeEntered:
                    Logger.Debug($"{pCallback.m_ulSteamIDMakingChange} has joined the lobby");
                    SteamAPI.PlayerConnected();
                    break;
                case (uint)EChatMemberStateChange.k_EChatMemberStateChangeDisconnected:
                case (uint)EChatMemberStateChange.k_EChatMemberStateChangeLeft:
                    Logger.Debug($"{pCallback.m_ulSteamIDMakingChange} has left the lobby");
                    SteamAPI.DisconnectPlayer(pCallback.m_ulSteamIDMakingChange);
                    break;
                case (uint)EChatMemberStateChange.k_EChatMemberStateChangeBanned:
                    Logger.Debug($"{pCallback.m_ulSteamIDMakingChange} banned {pCallback.m_ulSteamIDUserChanged} from the lobby");
                    SteamAPI.DisconnectPlayer(pCallback.m_ulSteamIDUserChanged);
                    break;
                case (uint)EChatMemberStateChange.k_EChatMemberStateChangeKicked:
                    Logger.Debug($"{pCallback.m_ulSteamIDMakingChange} kicked {pCallback.m_ulSteamIDUserChanged} from the lobby");
                    SteamAPI.DisconnectPlayer(pCallback.m_ulSteamIDUserChanged);
                    break;
            }
        }

        public static void OnLobbyEnter(LobbyEnter_t pCallback)
        {
            Logger.Debug($"You have entered lobby {pCallback.m_ulSteamIDLobby}");
            Controllers.PlayerController.Instance.StartBroadcasting();
            SteamAPI.SetConnectionState(SteamAPI.ConnectionState.CONNECTED);
            SteamAPI.SendPlayerPacket(Controllers.PlayerController.Instance._playerInfo);
            LobbyPacket info = new LobbyPacket(SteamMatchmaking.GetLobbyData(new CSteamID(pCallback.m_ulSteamIDLobby), "LOBBY_INFO"));
            SteamAPI.UpdateLobbyPacket(info);
            if (info.Screen == LobbyPacket.SCREEN_TYPE.IN_GAME && info.CurrentSongOffset > 0f)
            {
                WaitingMenu.autoReady = true;
                WaitingMenu.timeRequestedToLaunch = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds();
                WaitingMenu.Instance.Present();
            }
        }

        public void OnP2PSessionFail(P2PSessionConnectFail_t pCallback)
        {
            Logger.Error($"[P2PSessionConnectFail] Could not send packet to {pCallback.m_steamIDRemote}: {pCallback}");
        }

        public void OnP2PSessionRequest(P2PSessionRequest_t pCallback)
        {
            try
            {
                Logger.Debug($"{pCallback.m_steamIDRemote} requested a P2P session");

                int numMembers = SteamMatchmaking.GetNumLobbyMembers(SteamAPI.getLobbyID());
                for (int i = 0; i < numMembers; i++)
                {
                    var member = SteamMatchmaking.GetLobbyMemberByIndex(SteamAPI.getLobbyID(), i);
                    if (member.m_SteamID == pCallback.m_steamIDRemote.m_SteamID)
                    {
                        Logger.Debug($"{pCallback.m_steamIDRemote} is in our lobby, lets accept their P2P session");
                        bool ret = SteamNetworking.AcceptP2PSessionWithUser(pCallback.m_steamIDRemote);
                    }
                }
            } catch (Exception e)
            {
                Logger.Error(e);
            }
        }
    }
}
