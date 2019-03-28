﻿using IllusionPlugin;
using UnityEngine.SceneManagement;
using BeatSaberOnline.Data;
using BeatSaberOnline.Views;
using System;
using BeatSaberOnline.Controllers;
using SteamAPI = BeatSaberOnline.Data.Steam.SteamAPI;
using BeatSaberOnline.Workers;
using Harmony;
using System.Reflection;

namespace BeatSaberOnline
{
    public class Plugin : IPlugin
    {
        public static Plugin instance;
        public string Name => "BeatSaberOnline";
        public string Version => "1.0.4";
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
            new Config($"./UserData/{Name}.ini");
            Sprites.Init();

            SceneManager.sceneLoaded += SceneLoaded;
            SceneManager.activeSceneChanged += ActiveSceneChanged;

            var harmony = HarmonyInstance.Create("ca.vanzeben.beatsaber.beatsaberonline");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
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
            Logger.Debug($"Scene loaded: \"{to.name}\"");

            if (to.name == "MenuCore")
            {
                PluginUI.Init();

                GameController.Init(to);
                LeaderboardController.Init(to);
                Controllers.PlayerController.Init(to);
                VoiceChatWorker.Init();
            }

            try
            {
                if (to.name == "MenuCore" && SteamAPI.getLobbyID().m_SteamID > 0)
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
            GameController.Instance?.ActiveSceneChanged(from, to);
            LeaderboardController.Instance?.ActiveSceneChanged(from, to);
        }
    }
}
