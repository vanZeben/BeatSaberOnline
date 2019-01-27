using IllusionPlugin;
using UnityEngine.SceneManagement;
using BeatSaberOnline.Data;
using BeatSaberOnline.Views;
using System;
using Steamworks;
using BeatSaberOnline.Controllers;
using BeatSaberOnline.Views.Menus;
using SteamAPI = BeatSaberOnline.Data.Steam.SteamAPI;
using BeatSaberOnline.Data.Steam;

namespace BeatSaberOnline
{
    public class Plugin : IPlugin
    {
        public static Plugin instance;
        public string Name => "BeatSaberOnline";
        public string Version => "0.0.1";
        public string CurrentScene { get; set; }
        public void OnApplicationStart()
        {
            Init();
        }

        private void Init()
        {
            instance = this;
            Logger.Init();
            Config.Init();
            Sprites.Init();

            SceneManager.sceneLoaded += SceneLoaded;
            SceneManager.activeSceneChanged += ActiveSceneChanged;
        }

        public void OnApplicationQuit()
        {

            SceneManager.activeSceneChanged -= ActiveSceneChanged;
        }

        public void OnLevelWasLoaded(int level)
        {
        }

        public void OnLevelWasInitialized(int level)
        {
        }

        public void OnUpdate()
        {
        }

        public void OnFixedUpdate()
        {
        }

        private void SceneLoaded(Scene to, LoadSceneMode mode)
        {
        }
        private void ActiveSceneChanged(Scene from, Scene to)
        {
            Logger.Debug($"Active scene changed from \"{from.name}\" to \"{to.name}\"");
            CurrentScene = to.name;
            if (from.name == "EmptyTransition" && to.name == "Menu")
            {
                PluginUI.Init();

                Controllers.GameController.Init(to);
                Controllers.LeaderboardController.Init(to);
                Controllers.PlayerController.Init(to);
            }
            else
            { 
                GameController.Instance?.ActiveSceneChanged(from, to);
                LeaderboardController.Instance?.ActiveSceneChanged(from, to);
            }
        }
    }
}
