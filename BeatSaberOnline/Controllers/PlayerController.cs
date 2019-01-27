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
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR;
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
                Scoreboard.Instance.AddScoreboardEntry(_playerInfo.playerId, _playerInfo.playerName);

                InvokeRepeating("BroadcastPlayerInfo", 0f, GameController.TPS);
            }
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
                Scoreboard.Instance.UpdateScoreboardEntry(_playerInfo.playerId, (int)_playerInfo.playerScore, (int)_playerInfo.playerComboBlocks);
            }
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
        public Dictionary<string, bool> GetConnectedPlayerDownloadStatus()
        {
            Dictionary<string, bool> connectedPlayerStatus = new Dictionary<string, bool>();
            connectedPlayerStatus.Add(_playerInfo.playerName, _playerInfo.Downloading);
            for(int i = 0; i <  _connectedPlayers.Count;i++)
            {
                PlayerInfo info = _connectedPlayers.Values.ToArray()[i];
                connectedPlayerStatus.Add(info.playerName, info.Downloading);
            }
            return connectedPlayerStatus;
        }
        public void UpsertPlayer(PlayerInfo info)
        {
            try
            {
                if (!_connectedPlayerAvatars.Keys.Contains(info.playerId) && !_connectedPlayers.Keys.Contains(info.playerId))
                {
                    _connectedPlayers.Add(info.playerId, info);
                    AvatarController avatar = new GameObject("AvatarController").AddComponent<AvatarController>();
                    avatar.SetPlayerInfo(info, 0, info.playerId == _playerInfo.playerId);
                    _connectedPlayerAvatars.Add(info.playerId, avatar);

                    Scoreboard.Instance.AddScoreboardEntry(info.playerId, info.playerName);
                    return;
                }

                if (_connectedPlayers.ContainsKey(info.playerId) && _connectedPlayerAvatars.ContainsKey(info.playerId))
                {
                    bool refresh = false;
                    if (_connectedPlayers[info.playerId].Downloading != info.Downloading)
                    {
                        refresh = true;
                        if (info.Downloading)
                        {

                            if (!SteamAPI.IsHost())
                            {
                                if (_connectedPlayers.Values.ToList().All(u => u.Downloading))
                                {
                                    Data.Logger.Debug($"Everyone has confirmed that they are ready to play, broadcast that we want them all to start playing");
                                    SteamAPI.StartPlaying();
                                }
                            }
                        } else
                        {
                            if (SteamAPI.IsHost())
                            {
                                if (_connectedPlayers.Values.ToList().All(u => !u.Downloading))
                                {
                                    Data.Logger.Debug($"Everyone has confirmed they are in game, set the lobby screen to in game");

                                    SteamAPI.StartGame();
                                }
                            }
                        }
                    }
                    if (info.playerScore != _connectedPlayers[info.playerId].playerScore || info.playerComboBlocks != _connectedPlayers[info.playerId].playerComboBlocks)
                    {
                        Scoreboard.Instance.UpdateScoreboardEntry(info.playerId, (int)info.playerScore, (int)info.playerComboBlocks);
                    }
                    int offset = 0;
                    if (Plugin.instance.CurrentScene == "GameCore")
                    {
                        ulong[] playerInfosByID = new ulong[_connectedPlayers.Count + 1];
                        playerInfosByID[0] = _playerInfo.playerId;
                        _connectedPlayers.Keys.ToList().CopyTo(playerInfosByID, 1);
                        Array.Sort(playerInfosByID);
                        offset = (Array.IndexOf(playerInfosByID, info.playerId) - Array.IndexOf(playerInfosByID, _playerInfo.playerId)) * 3;
                    }

                    _connectedPlayers[info.playerId] = info;
                    _connectedPlayerAvatars[info.playerId].SetPlayerInfo(info, offset, info.playerId == _playerInfo.playerId);
                    if (refresh)
                    {
                        WaitingMenu.RefreshData(false);
                    }
                }
            } catch (Exception e)
            {
                Data.Logger.Error(e);
            }
        }
        void BroadcastPlayerInfo()
        {
            UpdatePlayerInfo();
            SteamAPI.SendPlayerInfo(_playerInfo);
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

    }
}
