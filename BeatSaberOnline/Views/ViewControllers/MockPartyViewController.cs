using BeatSaberOnline.Data;
using BeatSaberOnline.Data.Steam;
using CustomUI.BeatSaber;
using CustomUI.Utilities;
using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using Logger = BeatSaberOnline.Data.Logger;

namespace BeatSaberOnline.Views.ViewControllers
{
    class MockPartyViewController
    {
        private PartyFreePlayFlowCoordinator _partyFlowCoordinator;
        private StandardLevelDetailViewController detail;
        private GameplaySetupViewController _gameplaySetupViewController;
        private Button mPlay;

        public MockPartyViewController()
        {
            _partyFlowCoordinator = Resources.FindObjectsOfTypeAll<PartyFreePlayFlowCoordinator>().FirstOrDefault();

            LevelListViewController level = ReflectionUtil.GetPrivateField<LevelListViewController>(_partyFlowCoordinator, "_levelListViewController");
             detail = ReflectionUtil.GetPrivateField<StandardLevelDetailViewController>(_partyFlowCoordinator, "_levelDetailViewController");
            BeatmapDifficultyViewController beatmap = ReflectionUtil.GetPrivateField<BeatmapDifficultyViewController>(_partyFlowCoordinator, "_beatmapDifficultyViewController");
             _gameplaySetupViewController = ReflectionUtil.GetPrivateField<GameplaySetupViewController>(_partyFlowCoordinator, "_gameplaySetupViewController");
            
            ReflectionUtil.SetField(level, "didSelectLevelEvent", null);
            level.didSelectLevelEvent += didSelectLevel;
            
            beatmap.didSelectDifficultyEvent += didSelectBeatmap;

            mPlay = BeatSaberUI.CreateUIButton(detail.rectTransform, "CreditsButton", new Vector2(0f, -24f), new Vector2(40, 9f));

            mPlay.SetButtonText("Play with Lobby");
            mPlay.SetButtonTextSize(5f);
            mPlay.gameObject.SetActive(false);
            mPlay.ToggleWordWrapping(false);

            mPlay.onClick.AddListener(didSelectPlay);
        }

        private void toggleButtons(bool val)
        {
            Button play = ReflectionUtil.GetPrivateField<Button>(detail, "_playButton");
            Button practice = ReflectionUtil.GetPrivateField<Button>(detail, "_practiceButton");
            if (Data.Steam.SteamAPI.GetConnectionState() != SteamAPI.ConnectionState.CONNECTED || (!_partyFlowCoordinator || !_partyFlowCoordinator.isActivated))
            {
                play.gameObject.SetActive(true);
                play.gameObject.SetActiveRecursively(true); // something else in another plugin/base game is calling this and we need to forcibly override it 
                practice.gameObject.SetActive(true);
                play.interactable = true;
                practice.interactable = true;
                mPlay.gameObject.SetActive(false);
                return;
            }
            if (play && play.gameObject)
            {
                play.gameObject.SetActive(val);
                play.gameObject.SetActiveRecursively(false); // something else in another plugin/base game is calling this and we need to forcibly override it 
                play.interactable = false;
            }
            if (practice && practice.gameObject)
            {
                practice.gameObject.SetActive(val);
                practice.interactable = false;
            }
            if (mPlay && mPlay.gameObject)
            {
                mPlay.gameObject.SetActive(!val);
            }
            if (mPlay && !SteamAPI.IsHost())
            {
                mPlay.SetButtonText("You need to be host");
                mPlay.interactable = false;
            }
        }

        private void didSelectPlay()
        {try
            {
                var practice = ReflectionUtil.GetPrivateField<Button>(detail, "_practiceButton");
                Logger.Debug("Custom play button selected");
                if (!_partyFlowCoordinator || !_partyFlowCoordinator.isActivated)
                {
                    toggleButtons(true);
                    return;
                }
                toggleButtons(false);
                SteamAPI.RequestPlay(new GameplayModifiers(_gameplaySetupViewController.gameplayModifiers));
            } catch (Exception e)
            {
                Logger.Error(e);
            }
        }

        private void didSelectBeatmap(BeatmapDifficultyViewController controller, IDifficultyBeatmap beatmap)
        {
            Logger.Debug($"beatmap {beatmap.difficulty} selected");

            if (!_partyFlowCoordinator || !_partyFlowCoordinator.isActivated)
            {
                toggleButtons(true);
                return;
            }
            toggleButtons(false);
            SteamAPI.SetDifficulty((byte)beatmap.difficulty);

        }

        protected void didSelectLevel(LevelListViewController controller, IBeatmapLevel level)
        {
            Logger.Debug($"level {level.levelID} selected");

            if (!_partyFlowCoordinator || !_partyFlowCoordinator.isActivated)
            {
                toggleButtons(true);
                return;
            }
            toggleButtons(false);
            SteamAPI.SetSong(level.levelID, level.songName);
        }
    }
}
