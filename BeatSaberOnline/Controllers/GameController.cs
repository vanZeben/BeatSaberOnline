using BeatSaberOnline.Data;
using BeatSaberOnline.Utils;
using BeatSaberOnline.Views.Menus;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using VRUI;
using Logger = BeatSaberOnline.Data.Logger;
using SteamAPI = BeatSaberOnline.Data.Steam.SteamAPI;

namespace BeatSaberOnline.Controllers
{
    class GameController : MonoBehaviour
    {
        public static GameController Instance;
        public static float TPS_MODIFIER = 1;
        public static float TPS
        {
            get
            {
                float tps = 0;
                switch (Config.Instance.NetworkQuality)
                {
                    case 0:
                        tps = 5;
                        break;
                    case 1:
                        tps = 10;
                        break;
                    case 2:
                        tps = 15;
                        break;
                    case 3:
                        tps = 20;
                        break;
                    case 4:
                        tps = 25;
                        break;
                    case 5:
                        tps = 30;
                        break;
                }
                tps *= TPS_MODIFIER;
                return 1f / tps;
            }
        }
        private ResultsViewController _resultsViewController;
        private string _currentScene;
        
        public static void Init(Scene to)
        {
            if (Instance != null)
            {
                return;
            }
            
            new GameObject("InGameOnlineController").AddComponent<GameController>();
        }

        public void Awake()
        {
            if (Instance != this)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);

                _resultsViewController = Resources.FindObjectsOfTypeAll<ResultsViewController>().First();
                Scoreboard.OnLoad();
                _currentScene = SceneManager.GetActiveScene().name;
            }
        }
        

        IEnumerator RunLobbyCleanup()
        {
            yield return new WaitUntil(delegate () { return WaitingMenu.Instance.isActiveAndEnabled; });
            Logger.Debug("Finished song, doing cleanup");
            WaitingMenu.Instance.Dismiss();
            WaitingMenu.firstInit = true;
            WaitingMenu.queuedSong = null;
            WaitingMenu.autoReady = false;
            SongListUtils.InSong = false;
            Controllers.PlayerController.Instance._playerInfo.InSong = false;
            SteamAPI.SetSongOffset(0f);
            CancelInvoke("UpdateSongOffset");
            SteamAPI.FinishSong();
        }
        private AudioTimeSyncController timeSync;
        public void UpdateSongOffset()
        {
            if (!timeSync)
            {
                timeSync = Resources.FindObjectsOfTypeAll<AudioTimeSyncController>().First();
            }
            else
            {
                if ( (timeSync.songLength - timeSync.songTime) > 30)
                {
                    SteamAPI.SetSongOffset(timeSync.songTime);
                }
                else
                {
                    SteamAPI.SetSongOffset(0f);
                    CancelInvoke("UpdateSongOffset");
                }
            }
        }
       
            public void ActiveSceneChanged(Scene from, Scene to)
        {
            try
            {
                if (!SteamAPI.isLobbyConnected())
                {
                    return;
                }
                if (to.name == "GameCore" || to.name == "Menu")
                {
                    try
                    {
                        PlayerController.Instance.DestroyAvatars();
                        if (to.name == "GameCore" && SongListUtils.InSong)
                        {
                            SteamAPI.StartGame();
                            InvokeRepeating("UpdateSongOffset", 0f, 1f);
                            Scoreboard.Instance.disabled = false;
                            List<PlayerPacket> connectedPlayers = Controllers.PlayerController.Instance.GetConnectedPlayerPackets();
                            for (int i = 0; i < connectedPlayers.Count; i++)
                            {
                                Scoreboard.Instance.UpsertScoreboardEntry(connectedPlayers[i].playerId, connectedPlayers[i].playerName, 0, 0);
                            }
                        }
                        else if (to.name == "Menu")
                        {
                            Scoreboard.Instance.RemoveAll();
                            Scoreboard.Instance.disabled = true;
                            if (from.name == "GameCore" && SongListUtils.InSong)
                            {
                                StartCoroutine(RunLobbyCleanup());
                            }
                        }
                    } catch(Exception e)
                    {
                        Logger.Error(e);
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Error($"(OnlineController) Exception on {_currentScene} scene activation! Exception: {e}");
            }
        }
        private FlowCoordinator GetActiveFlowCoordinator()
        {
            FlowCoordinator[] flowCoordinators = Resources.FindObjectsOfTypeAll<FlowCoordinator>();
            foreach (FlowCoordinator f in flowCoordinators)
            {
                if (f.isActivated)
                    return f;
            }
            return null;
        }
        
        public void SongFinished(StandardLevelSceneSetupDataSO sender, LevelCompletionResults levelCompletionResults, IDifficultyBeatmap difficultyBeatmap, GameplayModifiers gameplayModifiers)
        {
            try
            {
                if (sender == null || levelCompletionResults == null || difficultyBeatmap == null || gameplayModifiers == null) { return; }
                Logger.Debug("Finished song: " + levelCompletionResults.levelEndStateType + " - " + levelCompletionResults.songDuration + " - - " + levelCompletionResults.endSongTime);

                PlayerDataModelSO _playerDataModel = Resources.FindObjectsOfTypeAll<PlayerDataModelSO>().First();
                _playerDataModel.currentLocalPlayer.playerAllOverallStatsData.soloFreePlayOverallStatsData.UpdateWithLevelCompletionResults(levelCompletionResults);
                _playerDataModel.Save();
                if (levelCompletionResults.levelEndStateType == LevelCompletionResults.LevelEndStateType.Failed)
                {
                    Controllers.PlayerController.Instance._playerInfo.SongFailed = true;
                }
                if (levelCompletionResults.levelEndStateType != LevelCompletionResults.LevelEndStateType.Failed && levelCompletionResults.levelEndStateType != LevelCompletionResults.LevelEndStateType.Cleared)
                {
                    return;
                }

                PlayerDataModelSO.LocalPlayer currentLocalPlayer = _playerDataModel.currentLocalPlayer;
                bool cleared = levelCompletionResults.levelEndStateType == LevelCompletionResults.LevelEndStateType.Cleared;
                string levelID = difficultyBeatmap.level.levelID;
                BeatmapDifficulty difficulty = difficultyBeatmap.difficulty;
                PlayerLevelStatsData playerLevelStatsData = currentLocalPlayer.GetPlayerLevelStatsData(levelID, difficulty);
                bool newHighScore = playerLevelStatsData.highScore < levelCompletionResults.score;
                playerLevelStatsData.IncreaseNumberOfGameplays();
                if (cleared)
                {
                    playerLevelStatsData.UpdateScoreData(levelCompletionResults.score, levelCompletionResults.maxCombo, levelCompletionResults.fullCombo, levelCompletionResults.rank);
                    Resources.FindObjectsOfTypeAll<PlatformLeaderboardsModel>().First().AddScore(difficultyBeatmap, levelCompletionResults.unmodifiedScore, gameplayModifiers);
                }
            } catch (Exception e)
            {
                Data.Logger.Error(e);
            }

        }

    }

}
