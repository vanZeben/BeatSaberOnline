using BeatSaberOnline.Controllers;
using BeatSaberOnline.Views.Menus;
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


        public SteamCallbacks()
        {

            m_GameLobbyJoinRequested = Callback<GameLobbyJoinRequested_t>.Create(OnGameLobbyJoinRequested);
            m_LobbyEnter_t = Callback<LobbyEnter_t>.Create(OnLobbyEnter);
            m_LobbyDataUpdate_t = Callback<LobbyDataUpdate_t>.Create(OnLobbyDataUpdate);
            m_P2PSessionRequest = Callback<P2PSessionRequest_t>.Create(OnP2PSessionRequest);
            m_P2PSessionConnectFail_t = Callback<P2PSessionConnectFail_t>.Create(OnP2PSessionFail);
        }

        private void OnGameLobbyJoinRequested(GameLobbyJoinRequested_t pCallback)
        {
            SteamAPI.JoinLobby(pCallback.m_steamIDLobby);
            if (OnlineMenu.Instance == null)
            {
                OnlineMenu.Init();
            }
            OnlineMenu.Instance.Present();
        }

        public void OnLobbyDataUpdate(LobbyDataUpdate_t pCallback)
        {
            if (pCallback.m_ulSteamIDLobby == pCallback.m_ulSteamIDMember)
            {
                string songId = SteamMatchmaking.GetLobbyData(new CSteamID(pCallback.m_ulSteamIDLobby), "SongId");
                string songDifficulty = SteamMatchmaking.GetLobbyData(new CSteamID(pCallback.m_ulSteamIDLobby), "SongDifficulty");
                string screen = SteamMatchmaking.GetLobbyData(new CSteamID(pCallback.m_ulSteamIDLobby), "Screen");
                SteamAPI.UpdateSongData(songId, Encoding.ASCII.GetBytes(songDifficulty)[0]);
                if (screen == "WAITING")
                {
                    WaitingMenu.Instance.Present();
                    Logger.Info($"Song request to be played -- ${songId} -- ${songDifficulty}");
                } else if (screen == "MENU")
                {
                    GameController.Instance.SongFinished(null, null, null, null);
                }
            } else
            {
                string readyStatus = SteamMatchmaking.GetLobbyMemberData(new CSteamID(pCallback.m_ulSteamIDLobby), new CSteamID(pCallback.m_ulSteamIDMember), "ReadyStatus");
                if (readyStatus == "Ready")
                {
                    SteamAPI.SetPlayerReady(new CSteamID(pCallback.m_ulSteamIDMember));
                }
            }
        }

        public static void OnLobbyEnter(LobbyEnter_t pCallback)
        {
            SteamAPI.SetConnectionState(SteamAPI.ConnectionState.CONNECTED);
            SteamAPI.SendPlayerInfo(Controllers.PlayerController.Instance._playerInfo);
            if (OnlineMenu.Instance == null)
            {
                OnlineMenu.Init();
            }
            OnlineMenu.Instance.Present();
            
        }

        public void OnP2PSessionFail(P2PSessionConnectFail_t pCallback)
        {
            Logger.Error($"[P2PSessionConnectFail] Could not send packet to {pCallback.m_steamIDRemote}: {pCallback}");
        }

        public void OnP2PSessionRequest(P2PSessionRequest_t pCallback)
        {
            try
            {
                int numMembers = SteamMatchmaking.GetNumLobbyMembers(SteamAPI.getLobbyID());
                for (int i = 0; i < numMembers; i++)
                {
                    var member = SteamMatchmaking.GetLobbyMemberByIndex(SteamAPI.getLobbyID(), i);
                    if (member.m_SteamID == pCallback.m_steamIDRemote.m_SteamID)
                    {
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
