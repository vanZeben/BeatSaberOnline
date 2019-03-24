using BeatSaberOnline.Controllers;
using BeatSaberOnline.Data.Steam;
using BeatSaberOnline.Views.Menus;
using IllusionInjector;
using IllusionPlugin;
using SongLoaderPlugin;
using System;
using System.Linq;
using UnityEngine;

namespace BeatSaberOnline.Utils
{
    class SongListUtils
    {
        private static LevelPackLevelsViewController _standardLevelListViewController = null;
        private static bool _initialized = false;
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
            _standardLevelListViewController = Resources.FindObjectsOfTypeAll<LevelPackLevelsViewController>().FirstOrDefault();

            if(!_initialized)
            {
                try
                {
                    _songDownloaderInstalled = IsModInstalled("BeatSaver Downloader");
                    _initialized = true;
                } catch (Exception e)
                {
                    Data.Logger.Error($"Exception {e}");
                }
            }
        }
        public static bool InSong = false;
        private static IDifficultyBeatmap _difficultyBeatmap;
        private static GameplayModifiers _gameplayModifiers;
        public static void StartSong(BeatmapLevelSO level, byte difficulty, GameplayModifiers gameplayModifiers, PracticeSettings practiceSettings = null)
        {
            if (InSong || level == null|| gameplayModifiers == null) { return; }
            try
            {
                MenuTransitionsHelperSO menuSceneSetupData = Resources.FindObjectsOfTypeAll<MenuTransitionsHelperSO>().FirstOrDefault();
                if (menuSceneSetupData != null)
                {
                    PlayerSpecificSettings playerSettings = Resources.FindObjectsOfTypeAll<PlayerDataModelSO>().FirstOrDefault().currentLocalPlayer.playerSpecificSettings;
                    _gameplayModifiers = gameplayModifiers;
                    _difficultyBeatmap = level.difficultyBeatmapSets[0].difficultyBeatmaps[difficulty];
                    
                    Data.Logger.Debug($"Starting song: name={level.songName}, levelId={level.levelID}, difficulty={_difficultyBeatmap.difficulty}");
                    InSong = true;
                    Controllers.PlayerController.Instance._playerInfo.SongFailed = false;
                    Controllers.PlayerController.Instance._playerInfo.InSong = true;
                    menuSceneSetupData.StartStandardLevel(_difficultyBeatmap, gameplayModifiers, playerSettings, practiceSettings, null, new Action<StandardLevelScenesTransitionSetupDataSO, LevelCompletionResults>(FinishSong));
                }
            } catch (Exception e)
            {
                Data.Logger.Error(e);
            }
        }

        private static void FinishSong(StandardLevelScenesTransitionSetupDataSO sender, LevelCompletionResults levelCompletionResults)
        {
            GameController.Instance.SongFinished(sender, levelCompletionResults, _difficultyBeatmap, _gameplayModifiers);
        }
        
        public static BeatmapLevelSO GetInstalledSong(string levelId = null)
        {
            try
            {
                if (levelId == null)
                {
                    levelId = SteamAPI.GetSongId();
                }
                BeatmapLevelSO level;
                 if (levelId.Length > 32)
                 {
                     if (SongLoader.CustomLevels == null) { return null; }
                     BeatmapLevelSO[] levels = SongLoader.CustomLevels.Where(l => l.levelID.StartsWith(levelId.Substring(0, 32))).ToArray();
                     level = levels.Length > 0 ? levels[0] : null;
                 }
                 else
                 {
                if (SongLoader.CustomLevelCollectionSO._levelList == null) { return null; }
                BeatmapLevelSO[] levels = SongLoader.CustomLevelCollectionSO._levelList.Where(l => l.levelID.StartsWith(levelId)).ToArray();
                level = levels.Length > 0 ? levels[0] : null;
                 }
                return level;
            }
            catch (Exception e) {
                Data.Logger.Error(e);
                return null;
            }
        }

    }
}
