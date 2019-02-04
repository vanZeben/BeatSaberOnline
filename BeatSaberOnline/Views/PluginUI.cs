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

            var AutoStartLobby = settingsMenu.AddBool("Auto-Start Lobby", "Opens up a lobby when you launch the game");
            AutoStartLobby.GetValue += delegate { return Config.Instance.AutoStartLobby; };
            AutoStartLobby.SetValue += delegate (bool value) { Config.Instance.AutoStartLobby = value; };

            var IsPublic = settingsMenu.AddBool("Auto-Start Privacy", "Configures the privacy of lobbies that are auto-started");
            IsPublic.DisabledText = "Friends Only";
            IsPublic.EnabledText = "Public";
            IsPublic.GetValue += delegate { return Config.Instance.IsPublic; };
            IsPublic.SetValue += delegate (bool value) { Config.Instance.IsPublic = value; };

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
            
            var AvatarsInLobby = settingsMenu.AddBool("Enable Avatars In Lobby", "Turns avatars on for you in the waiting lobby");
            AvatarsInLobby.GetValue += delegate { return Config.Instance.AvatarsInLobby; };
            AvatarsInLobby.SetValue += delegate (bool value) { Config.Instance.AvatarsInLobby = value; };

            var AvatarsInGame = settingsMenu.AddBool("Enable Avatars In Game", "Turns avatars on for you while playing songs");
            AvatarsInGame.GetValue += delegate { return Config.Instance.AvatarsInGame; };
            AvatarsInGame.SetValue += delegate (bool value) { Config.Instance.AvatarsInGame = value; };
            
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

            var NetworkScaling = settingsMenu.AddBool("Network Scaling", "Scales your network traffic based on the size of your lobby.");
            NetworkScaling.GetValue += delegate { return Config.Instance.NetworkScaling; };
            NetworkScaling.SetValue += delegate (bool value) { Config.Instance.NetworkScaling = value; };
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
