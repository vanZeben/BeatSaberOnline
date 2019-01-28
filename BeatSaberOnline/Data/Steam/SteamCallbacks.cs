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

        private LobbyInfo.SCREEN_TYPE currentScreen;
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
        private bool DidScreenChange(LobbyInfo.SCREEN_TYPE newScreen, LobbyInfo.SCREEN_TYPE val)
        {
            return currentScreen != val && val == newScreen;
        }
        public void OnLobbyDataUpdate(LobbyDataUpdate_t pCallback)
        {
            if (pCallback.m_ulSteamIDLobby == pCallback.m_ulSteamIDMember)
            {
                LobbyInfo info = new LobbyInfo(SteamMatchmaking.GetLobbyData(new CSteamID(pCallback.m_ulSteamIDLobby), "LOBBY_INFO"));

                if (pCallback.m_ulSteamIDLobby == 0) return;
               
                if (pCallback.m_ulSteamIDLobby == SteamAPI.getLobbyID().m_SteamID)
                {

                    if (DidScreenChange(info.Screen, LobbyInfo.SCREEN_TYPE.WAITING))
                        {
                        Logger.Debug($"Song has been selected, going to the waiting screen");
                        WaitingMenu.Instance.Present();
                    }
                    else if (DidScreenChange(info.Screen, LobbyInfo.SCREEN_TYPE.MENU))
                    {
                        Logger.Debug($"Song has finished, updating state to menu");
                        GameController.Instance.SongFinished(null, null, null, null);
                    }
                    else if (DidScreenChange(info.Screen, LobbyInfo.SCREEN_TYPE.PLAY_SONG))
                    {
                        Logger.Debug($"Host requested to play the current song {info.CurrentSongId}");

                        LevelSO song = SongListUtils.GetInstalledSong();
                        if (SteamAPI.IsHost())
                        {
                            SteamAPI.setLobbyStatus("Playing " + song.songName + " by " + song.songAuthorName);
                        }

                        SteamAPI.ClearPlayerReady(new CSteamID(SteamAPI.GetUserID()), true);
                        SongListUtils.StartSong(song, SteamAPI.GetSongDifficulty(), info.GameplayModifiers);
                    }

                    SteamAPI.UpdateLobbyInfo(info);
                    currentScreen = info.Screen;
                } else
                {
                    SteamAPI.SetOtherLobbyData(pCallback.m_ulSteamIDLobby, info);
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
                    SteamAPI.PlayerDisconnected();
                    break;
                case (uint)EChatMemberStateChange.k_EChatMemberStateChangeBanned:
                    Logger.Debug($"{pCallback.m_ulSteamIDMakingChange} banned {pCallback.m_ulSteamIDUserChanged} from the lobby");
                    break;
                case (uint)EChatMemberStateChange.k_EChatMemberStateChangeKicked:
                    Logger.Debug($"{pCallback.m_ulSteamIDMakingChange} kicked {pCallback.m_ulSteamIDUserChanged} from the lobby");
                    break;
            }
        }

        public static void OnLobbyEnter(LobbyEnter_t pCallback)
        {

            Logger.Debug($"You have entered lobby {pCallback.m_ulSteamIDLobby}");
            SteamAPI.SetConnectionState(SteamAPI.ConnectionState.CONNECTED);
            SteamAPI.SendPlayerInfo(Controllers.PlayerController.Instance._playerInfo);            
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
