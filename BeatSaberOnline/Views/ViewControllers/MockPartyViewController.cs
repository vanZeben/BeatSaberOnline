using BeatSaberOnline.Data.Steam;
using BeatSaberOnline.Utils;
using BeatSaberOnline.Views.Menus;
using CustomUI.BeatSaber;
using CustomUI.Utilities;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Logger = BeatSaberOnline.Data.Logger;

namespace BeatSaberOnline.Views.ViewControllers
{
    public class MockPartyViewController
    {
        public static MockPartyViewController Instance;
        private MainMenuViewController _mainMenuController;
        private PartyFreePlayFlowCoordinator _partyFlowCoordinator;
        private StandardLevelDetailView detailView;
        private GameplaySetupViewController _gameplaySetupViewController;
        private bool songExists = false;

        public MockPartyViewController()
        {
            Instance = this;
            _partyFlowCoordinator = Resources.FindObjectsOfTypeAll<PartyFreePlayFlowCoordinator>().FirstOrDefault();
            LevelPackLevelsViewController levels = ReflectionUtil.GetPrivateField<LevelPackLevelsViewController>(_partyFlowCoordinator, "_levelPackLevelsViewController");
            LevelPacksViewController levelPacks = ReflectionUtil.GetPrivateField<LevelPacksViewController>(_partyFlowCoordinator, "_levelPacksViewController");
            LevelPackDetailViewController levelPackDetail = ReflectionUtil.GetPrivateField<LevelPackDetailViewController>(_partyFlowCoordinator, "_levelPackDetailViewController");
            StandardLevelDetailViewController detail = ReflectionUtil.GetPrivateField<StandardLevelDetailViewController>(_partyFlowCoordinator, "_levelDetailViewController");
            detailView = ReflectionUtil.GetPrivateField<StandardLevelDetailView>(detail, "_standardLevelDetailView");
            _gameplaySetupViewController = ReflectionUtil.GetPrivateField<GameplaySetupViewController>(_partyFlowCoordinator, "_gameplaySetupViewController");
            LocalLeaderboardViewController leaderboard = ReflectionUtil.GetPrivateField<LocalLeaderboardViewController>(_partyFlowCoordinator, "_localLeaderboardViewController");


            levels.didSelectLevelEvent += didSelectLevel;
            detail.didChangeDifficultyBeatmapEvent += didSelectDifficultyBeatmap;
            detail.didPressPlayButtonEvent += didPressPlay;

            levelPackDetail.didActivateEvent += (firstActivation, type) => showLeaderboard();
            leaderboard.didActivateEvent += (firstActivation, type) => showLeaderboard();

            _mainMenuController = Resources.FindObjectsOfTypeAll<MainMenuViewController>().FirstOrDefault();
            Button partyButton = ReflectionUtil.GetPrivateField<Button>(_mainMenuController, "_partyButton");
            HoverHint hint = Resources.FindObjectsOfTypeAll<HoverHint>().Where(x => x.text == "Play with your friends locally!").First();
            _mainMenuController.didActivateEvent += (first, type) =>
            {
                if (Data.Steam.SteamAPI.GetConnectionState() != SteamAPI.ConnectionState.CONNECTED)
                {
                    partyButton.SetButtonText("Party");
                    if (hint)
                    {
                        hint.text = "Play with your friends locally!";
                    }
                }
                else
                {
                    partyButton.SetButtonText("Online Lobby");
                    if (hint)
                    {
                        hint.text = "Play with your friends in your steam lobby!";
                    }
                   
                }
            };
        }

        public void showLeaderboard()
        {
            if (Data.Steam.SteamAPI.GetConnectionState() != SteamAPI.ConnectionState.CONNECTED || !_partyFlowCoordinator || !_partyFlowCoordinator.isActivated) { return; }
            _partyFlowCoordinator.InvokePrivateMethod("SetRightScreenViewController", new object[] { MultiplayerLobby.Instance.rightViewController, false });
        }

        public void UpdatePlayButton()
        {
            if (Data.Steam.SteamAPI.GetConnectionState() != SteamAPI.ConnectionState.CONNECTED || (!_partyFlowCoordinator || !_partyFlowCoordinator.isActivated))
            {
                return;
            }
            Button play = detailView.playButton;
            if (!SteamAPI.IsHost())
            {
                play.SetButtonText("You need to be host");
                play.interactable = false;
            }
            else if (!Controllers.PlayerController.Instance.AllPlayersInMenu())
            {
                play.SetButtonText("Players still in song");
                play.interactable = false;
            }
            else if (!songExists)
            {
                play.SetButtonText("Song not on BeatSaver");
                play.interactable = false;
            }
            else
            {
                play.SetButtonText("Play");
                play.interactable = true;
            }
        }
        private void toggleButtons(bool val)
        {
            try
            {
                Button practice = detailView.practiceButton;
                if (Data.Steam.SteamAPI.GetConnectionState() != SteamAPI.ConnectionState.CONNECTED || (!_partyFlowCoordinator || !_partyFlowCoordinator.isActivated))
                {
                    practice.gameObject.SetActive(true);
                    practice.interactable = true;
                    return;
                }
                if (practice && practice.gameObject)
                {
                    practice.gameObject.SetActive(val);
                    practice.interactable = false;
                }
                UpdatePlayButton();
            } catch(Exception e)
            {
                Data.Logger.Error(e);
            }
        }

        public void didPressPlay(StandardLevelDetailViewController controller)
        {
            Logger.Debug("press play");
            try
            {
                if (!SteamAPI.IsHost() || !Controllers.PlayerController.Instance.AllPlayersInMenu())
                {
                    return;
                }
                if (!_partyFlowCoordinator || !_partyFlowCoordinator.isActivated)
                {
                    toggleButtons(true);
                    return;
                }
                if (songExists)
                {
                    toggleButtons(false);
                    SteamAPI.RequestPlay(new GameplayModifiers(_gameplaySetupViewController.gameplayModifiers));
                }
            } catch (Exception e)
            {
                Logger.Error(e);
            }
        }

        private void didSelectDifficultyBeatmap(StandardLevelDetailViewController controller, IDifficultyBeatmap beatmap)
        {
            Logger.Debug($"select beatmap {beatmap.level.songName}");
            Logger.Debug($"select beatmap {beatmap.difficulty}");
            if (!_partyFlowCoordinator || !_partyFlowCoordinator.isActivated)
            {
                toggleButtons(true);
                return;
            }
            toggleButtons(false);
            SteamAPI.SetDifficulty((byte)beatmap.difficulty);
        }

        protected void didSelectLevel(LevelPackLevelsViewController controller, IPreviewBeatmapLevel level)
        {
            Logger.Debug($"select level {level.songName}");
            if (!_partyFlowCoordinator || !_partyFlowCoordinator.isActivated)
            {
                toggleButtons(true);
                return;
            }
            toggleButtons(false);
            SteamAPI.SetSong(level.levelID, level.songName);
            SongDownloader.Instance.StartCoroutine(SongDownloader.CheckSongExists(level.levelID, doesSongExist));
        }

        private void doesSongExist(bool exists)
        {
            songExists = exists;
            toggleButtons(false);
        }
    }
}
