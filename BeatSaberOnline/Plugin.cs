using IllusionPlugin;
using UnityEngine.SceneManagement;
using BeatSaberOnline.Data;
using BeatSaberOnline.Views;
using System;
using BeatSaberOnline.Controllers;
using SteamAPI = BeatSaberOnline.Data.Steam.SteamAPI;
using BeatSaberOnline.Workers;

namespace BeatSaberOnline
{
    public class Plugin : IPlugin
    {
        public static Plugin instance;
        public string Name => "BeatSaberOnline";
        public string Version => "1.0.3";
        public string UpdatedVersion { get; set; }
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
            SteamAPI.Disconnect();
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
            try
            {
                if (to.name == "Menu" && SteamAPI.getLobbyID().m_SteamID > 0)
                {
                    Logger.Debug("Creating new lobby");
                    SteamAPI.CreateLobby(!Config.Instance.IsPublic);
                    Controllers.PlayerController.Instance.DestroyAvatars();
                }
            } catch (Exception e)
            {
                Logger.Error(e);
            }
        }
        private void ActiveSceneChanged(Scene from, Scene to)
        {
            Logger.Debug($"Active scene changed from \"{from.name}\" to \"{to.name}\"");
            CurrentScene = to.name;
            if (from.name == "EmptyTransition" && to.name == "Menu")
            {
                PluginUI.Init();

                GameController.Init(to);
                LeaderboardController.Init(to);
                Controllers.PlayerController.Init(to);
                VoiceChatWorker.Init();
            }
            else
            { 
                GameController.Instance?.ActiveSceneChanged(from, to);
                LeaderboardController.Instance?.ActiveSceneChanged(from, to);
            }
        }
    }
}
