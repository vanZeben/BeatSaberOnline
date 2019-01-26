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

namespace BeatSaberOnline.Views
{
    class PluginUI : MonoBehaviour
    {
        public static PluginUI instance;

        private MainMenuViewController _mainMenuViewController;
        private RectTransform _mainMenuRectTransform;
        private MockPartyViewController _mockPartyViewController;
        private Button _onlineButton;

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
                DontDestroyOnLoad(this);
                instance = this;
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

                SongLoader.SongsLoadedEvent += SongsLoaded;

                SteamAPI.GetServers();

                SongListUtils.Initialize();
                OnlineMenu.Init();
                WaitingMenu.Init();
                CreateMainMenuButton(); 
                CreateSettingsMenu();
            }
            catch (Exception e)
            {
                Logger.Error($"Unable to create UI! Exception: {e}");
            }
        }


        public void SongsLoaded(SongLoader sender, List<CustomLevel> levels)
        {
            if (_onlineButton != null)
            {
                _onlineButton.interactable = true;
            }            
        }

        private void CreateSettingsMenu()
        {
            var settingsMenu = SettingsUI.CreateSubMenu(Plugin.instance.Name);

            var doTheThing = settingsMenu.AddBool("Do the thing");
            doTheThing.GetValue += delegate { return Config.Instance.DoTheThing; };
            doTheThing.SetValue += delegate (bool value) { Config.Instance.DoTheThing = value; };

        }

        private void CreateMainMenuButton()
        {
            _onlineButton = BeatSaberUI.CreateUIButton(_mainMenuRectTransform, "SoloFreePlayButton");
            _onlineButton.transform.SetParent(Resources.FindObjectsOfTypeAll<Button>().First(x => x.name == "SoloFreePlayButton").transform.parent);
            _onlineButton.transform.SetSiblingIndex(2);

            _onlineButton.SetButtonText("Online");
            _onlineButton.SetButtonIcon(Sprites.onlineIcon);
            _onlineButton.interactable = SongLoader.AreSongsLoaded;
            _onlineButton.onClick.AddListener(delegate ()
            {
                try
                {
                    OnlineMenu.Instance.Present();
                }
                catch (Exception e)
                {
                    Logger.Error($"Unable to present flow coordinator! Exception: {e}");
                }
            });
        }

    }
}
