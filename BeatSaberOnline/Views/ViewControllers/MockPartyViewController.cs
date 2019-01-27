using BeatSaberOnline.Data;
using BeatSaberOnline.Data.Steam;
using CustomUI.BeatSaber;
using CustomUI.Utilities;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Logger = BeatSaberOnline.Data.Logger;

namespace BeatSaberOnline.Views.ViewControllers
{
    class MockPartyViewController
    {
        private PartyFreePlayFlowCoordinator _partyFlowCoordinator;
        StandardLevelDetailViewController detail;
        private Button mPlay;

        public MockPartyViewController()
        {
            _partyFlowCoordinator = Resources.FindObjectsOfTypeAll<PartyFreePlayFlowCoordinator>().FirstOrDefault();

            LevelListViewController level = ReflectionUtil.GetPrivateField<LevelListViewController>(_partyFlowCoordinator, "_levelListViewController");
             detail = ReflectionUtil.GetPrivateField<StandardLevelDetailViewController>(_partyFlowCoordinator, "_levelDetailViewController");
            BeatmapDifficultyViewController beatmap = ReflectionUtil.GetPrivateField<BeatmapDifficultyViewController>(_partyFlowCoordinator, "_beatmapDifficultyViewController");

            ReflectionUtil.SetField(level, "didSelectLevelEvent", null);
            level.didSelectLevelEvent += didSelectLevel;
            
            beatmap.didSelectDifficultyEvent += didSelectBeatmap;

            mPlay = BeatSaberUI.CreateUIButton(detail.rectTransform, "CreditsButton", new Vector2(0f, -24f), new Vector2(40, 9f));

            mPlay.SetButtonText("Play with Lobby");
            mPlay.SetButtonTextSize(5f);
            mPlay.ToggleWordWrapping(false);

            mPlay.onClick.AddListener(didSelectPlay);
        }

        private void toggleButtons(bool val)
        {   
            var play = ReflectionUtil.GetPrivateField<Button>(detail, "_playButton");
            var practice = ReflectionUtil.GetPrivateField<Button>(detail, "_practiceButton");
            if (play && play.gameObject && play.gameObject.activeSelf != val)
            {
                play.gameObject.SetActive(val);
            }
            if (practice && practice.gameObject && practice.gameObject.activeSelf != val)
            {
                practice.gameObject.SetActive(val);
            }
            if (mPlay && mPlay.gameObject && mPlay.gameObject.activeSelf == val)
            {
                mPlay.gameObject.SetActive(!val);
            }
        }

        private void didSelectPlay()
        {
            Logger.Debug("Custom play button selected");
            if (!_partyFlowCoordinator || !_partyFlowCoordinator.isActivated)
            {
                toggleButtons(true);
                return;
            }
            toggleButtons(false);
            SteamAPI.RequestPlay();
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
            SteamAPI.SetSong(level.levelID);
        }
    }
}
