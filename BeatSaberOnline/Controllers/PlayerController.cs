using BeatSaberOnline.Data;
using BeatSaberOnline.Data.Steam;
using BeatSaberOnline.Utils;
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
            if (fieldName != "playerEnergy" || fieldName != "playerScore" || fieldName != "playerTotalBlocks" || fieldName != "playerCutBlocks" || fieldName != "playerComboBlocks") return;
            ReflectionUtil.SetField(_playerInfo, fieldName, (fieldName == "playerTotalBlocks" || fieldName == "playerCutBlocks" ? ReflectionUtil.GetField<uint>(_playerInfo, fieldName) + value : value));
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


        public void UpsertPlayer(PlayerInfo info)
        {
            try
            {
                if (!_connectedPlayerAvatars.Keys.Contains(info.playerId))
                {
                    AvatarController avatar = new GameObject("AvatarController").AddComponent<AvatarController>();
                    avatar.SetPlayerInfo(info, 0, info.playerId == _playerInfo.playerId);
                    _connectedPlayers.Add(info.playerId, info);
                    _connectedPlayerAvatars.Add(info.playerId, avatar);
                    return;
                }
                if (_connectedPlayers.ContainsKey(info.playerId) && _connectedPlayerAvatars.ContainsKey(info.playerId))
                {

                    int offset = 0;
                    if (_currentScene == "GameCore")
                    {
                        ulong[] playerInfosByID = new ulong[_connectedPlayers.Count + 1];
                        playerInfosByID[0] = _playerInfo.playerId;
                        _connectedPlayers.Keys.ToList().CopyTo(playerInfosByID, 1);
                        Array.Sort(playerInfosByID);
                        offset = (Array.IndexOf(playerInfosByID, info.playerId) - Array.IndexOf(playerInfosByID, _playerInfo.playerId)) * 3;
                    };

                    _connectedPlayers[info.playerId] = info;
                    _connectedPlayerAvatars[info.playerId].SetPlayerInfo(info, offset, info.playerId == _playerInfo.playerId);
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
                        if (info.playerId != SteamAPI.GetUserID())
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
