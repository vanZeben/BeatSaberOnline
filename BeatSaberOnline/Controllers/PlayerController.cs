using BeatSaberOnline.Data;
using BeatSaberOnline.Data.Steam;
using BeatSaberOnline.Utils;
using BeatSaberOnline.Views.Menus;
using CustomUI.Utilities;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using SteamAPI = BeatSaberOnline.Data.Steam.SteamAPI;

namespace BeatSaberOnline.Controllers
{
    class PlayerController : MonoBehaviour
    {
        public static PlayerController Instance;

        public PlayerInfo _playerInfo;
        private Dictionary<ulong, PlayerInfo> _connectedPlayers = new Dictionary<ulong, PlayerInfo>();
        private Dictionary<ulong, AvatarController> _connectedPlayerAvatars = new Dictionary<ulong, AvatarController>();
        private string _currentScene;
        private GameObject m_VoiceLoopback;
        public bool VoipEnabled = false;

        public void clearVoiceLoopback()
        {
            m_VoiceLoopback = null;
        }
        public static void Init(Scene to)
        {
            if (Instance != null)
            {
                return;
            }
            new GameObject("PlayerController").AddComponent<PlayerController>();
        }

        public void Awake()
        {
            if (Instance != this)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);

                _playerInfo = new PlayerInfo(SteamAPI.GetUserName(), SteamAPI.GetUserID());
                _currentScene = SceneManager.GetActiveScene().name;

            }
        }
        public bool isBroadcasting = false;
        public void StartBroadcasting()
        {
            if (isBroadcasting) { return; }
            isBroadcasting = true;
            InvokeRepeating("BroadcastPlayerInfo", 0f, GameController.TPS);
        }
        public void StopBroadcasting()
        {
            if (!isBroadcasting) { return; }
            isBroadcasting = false;
            CancelInvoke("BroadcastPlayerInfo");
        }

        public void RestartBroadcasting()
        {
            StopBroadcasting();
            StartBroadcasting();
        }
        public void DestroyAvatars()
        {
            try
            {
                foreach (KeyValuePair<ulong, AvatarController> players in _connectedPlayerAvatars)
                {
                    if (players.Value != null)
                    {
                        Destroy(players.Value.gameObject);
                    }
                }
                _connectedPlayers.Clear();
                _connectedPlayerAvatars.Clear();
            }
            catch (Exception e)
            {
                Data.Logger.Error($"Unable to destroy avatars! Exception: {e}");
            }
        }

        public void UpdatePlayerScoring(string fieldName, uint value)
        {
            if (fieldName != "playerEnergy" && fieldName != "playerScore" && fieldName != "playerTotalBlocks" && fieldName != "playerCutBlocks" && fieldName != "playerComboBlocks") return;
            ReflectionUtil.SetField(_playerInfo, fieldName, (fieldName == "playerTotalBlocks" || fieldName == "playerCutBlocks" ? ReflectionUtil.GetField<uint>(_playerInfo, fieldName) + value : value));
            if (fieldName == "playerScore")
            {
                Scoreboard.Instance.UpsertScoreboardEntry(_playerInfo.playerId, _playerInfo.playerName, (int)_playerInfo.playerScore, (int)_playerInfo.playerComboBlocks);
            }
        }

        public List<PlayerInfo> GetConnectedPlayerInfos()
        {
            List<PlayerInfo> scores = new List<PlayerInfo>();
            scores.AddRange(_connectedPlayers.Values.ToList<PlayerInfo>());
            scores.Add(_playerInfo);
            return scores;
        }

        public void UpdatePlayerInfo()
        {
            _playerInfo.avatarHash = ModelSaberAPI.cachedAvatars.FirstOrDefault(x => x.Value == CustomAvatar.Plugin.Instance.PlayerAvatarManager.GetCurrentAvatar()).Key;
            if (_playerInfo.avatarHash == null) _playerInfo.avatarHash = "FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF";

            _playerInfo.playerName = SteamAPI.GetUserName();
            _playerInfo.playerId = SteamAPI.GetUserID();

            WorldController.CharacterPosition pos = WorldController.GetCharacterInfo();
                _playerInfo.headPos = pos.headPos;
                _playerInfo.headRot = pos.headRot;
                _playerInfo.leftHandPos = pos.leftHandPos;
                _playerInfo.leftHandRot = pos.leftHandRot;
                _playerInfo.rightHandPos = pos.rightHandPos;
                _playerInfo.rightHandRot = pos.rightHandRot;
        }


        public bool AllPlayersInMenu()
        {
            bool InMenu = !_playerInfo.InSong;
    if (!InMenu)
            {
                Data.Logger.Info("You are in a song");
            }
            for (int i = 0; i < _connectedPlayers.Count; i++)
            {
                if (InMenu && _connectedPlayers.Values.ToArray()[i].InSong)
                {
                    Data.Logger.Info(_connectedPlayers.Values.ToArray()[i].playerName+" is in song");
                    InMenu = false;
                    break;
                }
            }
            return InMenu;
        }

        public Dictionary<string, float> GetConnectedPlayerDownloadStatus()
        {
            Dictionary<string, float> connectedPlayerStatus = new Dictionary<string, float>();
            connectedPlayerStatus.Add(_playerInfo.playerName, _playerInfo.Ready ? 1f : _playerInfo.playerProgress);
            for(int i = 0; i <  _connectedPlayers.Count;i++)
            {
                PlayerInfo info = _connectedPlayers.Values.ToArray()[i];
                connectedPlayerStatus.Add(info.playerName, info.Ready ? 1f : info.playerProgress);
            }
            return connectedPlayerStatus;
        }
        public void UpsertPlayer(PlayerInfo info)
        {
            if (info.playerId == _playerInfo.playerId) { return; }
            try
            {
                if (!_connectedPlayers.Keys.Contains(info.playerId))
                {
                    _connectedPlayers.Add(info.playerId, info);
                    if ((Config.Instance.AvatarsInLobby && Plugin.instance.CurrentScene == "Menu") || (Config.Instance.AvatarsInGame && Plugin.instance.CurrentScene == "GameCore"))
                    {
                        AvatarController avatar = new GameObject("AvatarController").AddComponent<AvatarController>();
                        avatar.SetPlayerInfo(info, new Vector3(0, 0, 0), info.playerId == _playerInfo.playerId);
                        _connectedPlayerAvatars.Add(info.playerId, avatar);
                    }
                    MultiplayerLobby.RefreshScores();
                    Scoreboard.Instance.UpsertScoreboardEntry(info.playerId, info.playerName);
                    return;
                }

                if (_connectedPlayers.ContainsKey(info.playerId))
                {
                    if (info.playerScore != _connectedPlayers[info.playerId].playerScore || info.playerComboBlocks != _connectedPlayers[info.playerId].playerComboBlocks)
                    {
                        Scoreboard.Instance.UpsertScoreboardEntry(info.playerId, info.playerName, (int)info.playerScore, (int)info.playerComboBlocks);
                        MultiplayerLobby.RefreshScores();
                    }
                    if (_connectedPlayerAvatars.ContainsKey(info.playerId) && (Config.Instance.AvatarsInLobby && Plugin.instance.CurrentScene == "Menu") || (Config.Instance.AvatarsInGame && Plugin.instance.CurrentScene == "GameCore"))
                    {
                        Vector3 offset = new Vector3(0, 0, 0);
                        if (Plugin.instance.CurrentScene == "GameCore")
                        {
                            ulong[] playerInfosByID = new ulong[_connectedPlayers.Count + 1];
                            playerInfosByID[0] = _playerInfo.playerId;
                            _connectedPlayers.Keys.ToList().CopyTo(playerInfosByID, 1);
                            Array.Sort(playerInfosByID);
                            offset = new Vector3((Array.IndexOf(playerInfosByID, info.playerId) - Array.IndexOf(playerInfosByID, _playerInfo.playerId)) * 2f, 0, Math.Abs((Array.IndexOf(playerInfosByID, info.playerId) - Array.IndexOf(playerInfosByID, _playerInfo.playerId)) * 2.5f));
                        }

                        _connectedPlayerAvatars[info.playerId].SetPlayerInfo(info, offset, info.playerId == _playerInfo.playerId);
                    }
                    bool changedReady = (_connectedPlayers[info.playerId].Ready != info.Ready || _connectedPlayers[info.playerId].playerProgress != info.playerProgress);
                    
                    _connectedPlayers[info.playerId] = info;
                    if (changedReady)
                    {
                        WaitingMenu.RefreshData();
                        if (SteamAPI.IsHost())
                        {
                            if (info.Ready)
                            {
                                if (_connectedPlayers.Values.ToList().All(u => u.Ready))
                                {
                                    Data.Logger.Debug($"Everyone has confirmed that they are ready to play, broadcast that we want them all to start playing");
                                    SteamAPI.StartPlaying();
                                }
                            }
                            else
                            {
                                if (_connectedPlayers.Values.ToList().All(u => !u.Ready))
                                {
                                    Data.Logger.Debug($"Everyone has confirmed they are in game, set the lobby screen to in game");
                                    SteamAPI.StartGame();
                                }
                            }
                        }
                    }
                }
            } catch (Exception e)
            {
                Data.Logger.Error(e);
            }
        }

        public List<ulong> GetConnectedPlayers()
        {
            return _connectedPlayers.Keys.ToList();
        }
        void BroadcastPlayerInfo()
        {
            try
            {
                UpdatePlayerInfo();
                SteamAPI.SendPlayerInfo(_playerInfo);
                if (_playerInfo.voip != null && _playerInfo.voip.Length > 0)
                {
                    _playerInfo.voip = new byte[0];
                }
            } catch (Exception e)
            {
                Data.Logger.Error(e);
            }
        }
        public void RemoveConnectedPlayer(ulong playerId)
        {
            _connectedPlayers.Remove(playerId);
            AvatarController avatar = _connectedPlayerAvatars[playerId];
            if (avatar != null)
            {
                Destroy(avatar.gameObject);
            }
            _connectedPlayerAvatars.Remove(playerId);
        }
        void Update()
        {
            uint size;
            try
            {
                while (SteamNetworking.IsP2PPacketAvailable(out size))
                {
                    var buffer = new byte[size];
                    uint bytesRead;
                    CSteamID remoteId;
                    if (SteamNetworking.ReadP2PPacket(buffer, size, out bytesRead, out remoteId))
                    {
                        var message = Encoding.UTF8.GetString(buffer).Replace(" ", "");
                        PlayerInfo info = new PlayerInfo(message);
                        if (info.voip != null && info.voip.Length > 0)
                        {
                           if (VoipEnabled) 
                            {
                                PlayVoip(info.voip);
                            }
                            info.voip = new byte[0];
                        }
                        if (info.playerId != SteamAPI.GetUserID() && SteamAPI.getLobbyID().m_SteamID != 0)
                        {
                            UpsertPlayer(info);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Data.Logger.Error(e);
            }
        }
        bool PlayVoip(byte[] voipPacket)
        {
            byte[] voipBuffer = new byte[11025 * 2];
            uint byteLength;

            if (SteamUser.DecompressVoice(voipPacket, (uint)voipPacket.Length, voipBuffer, (uint)voipBuffer.Length, out byteLength, 11025) == EVoiceResult.k_EVoiceResultOK && byteLength > 0)
            {
                AudioSource source;
                if (!m_VoiceLoopback)
                {
                    m_VoiceLoopback = new GameObject("Voice Loopback");
                    source = m_VoiceLoopback.AddComponent<AudioSource>();
                    DontDestroyOnLoad(source.gameObject);
                    source.clip = AudioClip.Create("Voice Clip", 11025, 1, 11025, false);
                    source.volume = 1.0f;
                }
                else
                {
                    source = m_VoiceLoopback.GetComponent<AudioSource>();
                }

                float[] voip = new float[11025];
                for (int i = 0; i < voip.Length; ++i)
                {
                    voip[i] = (short)(voipBuffer[i * 2] | voipBuffer[i * 2 + 1] << 8) / 32768.0f;
                }
                source.clip.SetData(voip, 0);
                source.Play();
                return true;
            }
            return false;
        }
    }
}
