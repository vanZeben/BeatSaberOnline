using BeatSaberOnline.Controllers;
using BeatSaverDownloader.UI;
using HMUI;
using IllusionInjector;
using IllusionPlugin;
using SongBrowserPlugin;
using SongBrowserPlugin.DataAccess;
using SongLoaderPlugin;
using SongLoaderPlugin.OverrideClasses;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BeatSaberOnline.Utils
{
    class SongListUtils
    {
        private static LevelListViewController _standardLevelListViewController = null;
        private static bool _initialized = false;
        private static bool _songBrowserInstalled = false;
        private static bool _songDownloaderInstalled = false;

        public static bool IsModInstalled(string modName)
        {
            foreach (IPlugin p in PluginManager.Plugins)
            {
                if (p.Name == modName)
                {
                    return true;
                }
            }
            return false;
        }
        public static void Initialize()
        {
            _standardLevelListViewController = Resources.FindObjectsOfTypeAll<LevelListViewController>().FirstOrDefault();

            if(!_initialized)
            {
                try
                {
                    _songBrowserInstalled = IsModInstalled("Song Browser");
                    _songDownloaderInstalled = IsModInstalled("BeatSaver Downloader");
                    _initialized = true;
                } catch (Exception e)
                {
                    Data.Logger.Error($"Exception {e}");
                }
            }
        }

        public static void StartSong(LevelSO level, byte difficulty, bool noFail)
        {
            try
            {
                MenuSceneSetupDataSO menuSceneSetupData = Resources.FindObjectsOfTypeAll<MenuSceneSetupDataSO>().FirstOrDefault();
                if (menuSceneSetupData != null)
                {
                    GameplayModifiers gameplayModifiers = new GameplayModifiers();
                    gameplayModifiers.noFail = noFail;
                    

                    PlayerSpecificSettings playerSettings = Resources.FindObjectsOfTypeAll<PlayerDataModelSO>().FirstOrDefault().currentLocalPlayer.playerSpecificSettings;
                    IDifficultyBeatmap difficultyBeatmap = level.GetDifficultyBeatmap((BeatmapDifficulty)difficulty);
                    
                    Data.Logger.Info($"Starting song: name={level.songName}, levelId={level.levelID}, difficulty={difficultyBeatmap.difficulty}");
                    menuSceneSetupData.StartStandardLevel(difficultyBeatmap, gameplayModifiers, playerSettings, null, null, 
                        (StandardLevelSceneSetupDataSO sender, LevelCompletionResults levelCompletionResults) =>
                    {
                        GameController.Instance.SongFinished(sender, levelCompletionResults, difficultyBeatmap, gameplayModifiers);
                    });
                }
            } catch (Exception e)
            {
                Data.Logger.Error(e);
            }
        }
        
    }
}
