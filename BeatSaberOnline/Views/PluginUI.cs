using BeatSaberOnline.Data;
using CustomUI.BeatSaber;
using CustomUI.Settings;
using CustomUI.Utilities;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Logger = BeatSaberOnline.Data.Logger;
using TMPro;
using BeatSaberOnline.Views.Menus;
using BeatSaberOnline.Data.Steam;
using BeatSaberOnline.Views.ViewControllers;
using BeatSaberOnline.Utils;
using SongLoaderPlugin;
using System.Collections.Generic;
using SongLoaderPlugin.OverrideClasses;
using CustomUI.MenuButton;
using Steamworks;
using SteamAPI = BeatSaberOnline.Data.Steam.SteamAPI;
using BeatSaberOnline.Controllers;

namespace BeatSaberOnline.Views
{
    class PluginUI : MonoBehaviour
    {
        public static PluginUI instance;

        private MainMenuViewController _mainMenuViewController;
        private RectTransform _mainMenuRectTransform;
        private MockPartyViewController _mockPartyViewController;
        public static MenuButton MultiplayerButton;

        public object SongInfo { get; private set; }

        public static void Init()
        {
            if (instance != null)
            {
                instance.CreateUI();
                return;
            }
            new GameObject(Plugin.instance.Name).AddComponent<PluginUI>();
        }

        public void Awake()
        {
            if (instance != this)
            {
                instance = this;

                instance.StartCoroutine(Utils.AutoUpdater.GetLatestVersionDownload());
                DontDestroyOnLoad(this);
                SteamAPI.Init();
                CreateUI();
            }
        }
        protected void CreateUI()
        {
            try
            {
                _mainMenuViewController = Resources.FindObjectsOfTypeAll<MainMenuViewController>().First();
                _mainMenuRectTransform = _mainMenuViewController.transform as RectTransform;

                _mockPartyViewController = new MockPartyViewController();

                if (Config.Instance.AutoStartLobby)
                {
                    SteamAPI.CreateLobby(!Config.Instance.IsPublic);
                }

                AvatarController.LoadAvatars();
                SongListUtils.Initialize();
                MultiplayerListing.Init();
                MultiplayerLobby.Init();
                WaitingMenu.Init();
                CreateMainMenuButton(); 
                CreateSettingsMenu();
            }
            catch (Exception e)
            {
                Logger.Error($"Unable to create UI! Exception: {e}");
            }
        }
        float _avatarState = 0;
        float _autoStart = 0;
        private void CreateSettingsMenu()
        {
            var settingsMenu = SettingsUI.CreateSubMenu(Plugin.instance.Name);

            var AutoStartLobby = settingsMenu.AddList("Auto-Start Lobby", new float[3] { 0, 1, 2});
            AutoStartLobby.GetValue += delegate { return _autoStart; };
            AutoStartLobby.SetValue += delegate (float value)
            {
                _autoStart = value;
                switch (value)
                {
                    default:
                    case 0:
                        Config.Instance.AutoStartLobby = false;
                        break;
                    case 1:
                        Config.Instance.AutoStartLobby = true;
                        Config.Instance.IsPublic = false;
                        break;
                    case 2:
                        Config.Instance.AutoStartLobby = true;
                        Config.Instance.IsPublic = true;
                        break;
                }
            };
            AutoStartLobby.FormatValue += delegate (float value)
            {
                switch (value)
                {
                    default:
                    case 0:
                        return "Disabled";
                    case 1:
                        return "Private Lobby";
                    case 2:
                        return "Public Lobby";
                }
            };
            

            var MaxLobbySite = settingsMenu.AddInt("Lobby Size", "Configure the amount of users you want to be able to join your lobby.", 2, 15, 1);
            MaxLobbySite.GetValue += delegate { return Config.Instance.MaxLobbySize; };
            MaxLobbySite.SetValue += delegate (int value) {
                SteamMatchmaking.SetLobbyMemberLimit(SteamAPI.getLobbyID(), value);
                Config.Instance.MaxLobbySize = value;
            };

            var Volume = settingsMenu.AddInt("Voice Volume", "Higher numbers are louder", 1, 20, 1);
            Volume.GetValue += delegate { return (int)Config.Instance.Volume; };
            Volume.SetValue += delegate (int value) {
                Controllers.PlayerController.UpdateVolume((float) value);
                Config.Instance.Volume = (float) value;
            };

            
            var Avatar = settingsMenu.AddList("Enable Avatars", new float[4] { 0, 1, 2, 3 });
            Avatar.GetValue += delegate { return _avatarState; };
            Avatar.SetValue += delegate (float value) { _avatarState = value;
                switch (value) {
                    default:
                    case 0:
                        Config.Instance.AvatarsInLobby = false;
                        Config.Instance.AvatarsInGame = false;
                        break;
                    case 1:
                        Config.Instance.AvatarsInLobby = true;
                        Config.Instance.AvatarsInGame = false;
                        break;
                    case 2:
                        Config.Instance.AvatarsInLobby = false;
                        Config.Instance.AvatarsInGame = true;
                        break;
                    case 3:
                        Config.Instance.AvatarsInLobby = true;
                        Config.Instance.AvatarsInGame = true;
                        break;
                }
            };
            Avatar.FormatValue += delegate (float value)
            {
                switch (value)
                {
                    default:
                    case 0:
                        return "Disabled";
                    case 1:
                        return "Lobby Only";
                    case 2:
                        return "InGame Only";
                    case 3:
                        return "Enabled";
                }
            };
            
            var NetworkQuality = settingsMenu.AddInt("Network Quality", "Higher number, smoother avatar. Note that this effects how you appear to others. ", 0, 5, 1);
            NetworkQuality.GetValue += delegate { return Config.Instance.NetworkQuality; };
            NetworkQuality.SetValue += delegate (int value) {
                Config.Instance.NetworkQuality = value;
                if (Controllers.PlayerController.Instance.isBroadcasting)
                {
                    Controllers.PlayerController.Instance.StopBroadcasting();
                    Controllers.PlayerController.Instance.StartBroadcasting();
                }
            };
        }

        private void CreateMainMenuButton()
        {
            MultiplayerButton = MenuButtonUI.AddButton($"Multiplayer", "", delegate ()
            {
                try
                {
                    if (SteamAPI.isLobbyConnected())
                    {
                        MultiplayerLobby.Instance.Present();
                    }
                    else
                    {
                        MultiplayerListing.Instance.Present();
                    }
                }
                catch (Exception e)
                {
                    Logger.Error($"Unable to present flow coordinator! Exception: {e}");
                }
            });
        }
    }
}
