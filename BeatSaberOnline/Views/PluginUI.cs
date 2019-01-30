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
        
        private void CreateSettingsMenu()
        {
            var settingsMenu = SettingsUI.CreateSubMenu(Plugin.instance.Name);

            var AutoStartLobby = settingsMenu.AddBool("Auto-Start Lobby");
            AutoStartLobby.GetValue += delegate { return Config.Instance.AutoStartLobby; };
            AutoStartLobby.SetValue += delegate (bool value) { Config.Instance.AutoStartLobby = value; };

            var IsPublic = settingsMenu.AddBool("Auto-Start Privacy");
            IsPublic.DisabledText = "Friends Only";
            IsPublic.EnabledText = "Public";
            IsPublic.GetValue += delegate { return Config.Instance.IsPublic; };
            IsPublic.SetValue += delegate (bool value) { Config.Instance.IsPublic = value; };

            var MaxLobbySite = settingsMenu.AddInt("Lobby Size", 2, 10, 1);
            MaxLobbySite.GetValue += delegate { return Config.Instance.MaxLobbySize; };
            MaxLobbySite.SetValue += delegate (int value) {
                SteamMatchmaking.SetLobbyMemberLimit(SteamAPI.getLobbyID(), value);
                Config.Instance.MaxLobbySize = value;
            };
            
            var DebugMode = settingsMenu.AddBool("Debug Mode");
            DebugMode.GetValue += delegate { return Config.Instance.DebugMode; };
            DebugMode.SetValue += delegate (bool value) { Config.Instance.DebugMode = value; };
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
