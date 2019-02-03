using BeatSaberOnline.Data;
using BeatSaberOnline.Data.Steam;
using BeatSaberOnline.Views.Menus;
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
        private MainMenuViewController _mainMenuController;
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
            
            level.didActivateEvent += (first, type) => {
                if (Data.Steam.SteamAPI.GetConnectionState() != SteamAPI.ConnectionState.CONNECTED || !_partyFlowCoordinator || !_partyFlowCoordinator.isActivated) { return; }
                _partyFlowCoordinator.InvokePrivateMethod("SetRightScreenViewController", new object[] { MultiplayerLobby.Instance.rightViewController, true});
            };
            level.didSelectLevelEvent += didSelectLevel;
            
            beatmap.didSelectDifficultyEvent += didSelectBeatmap;
            mPlay = BeatSaberUI.CreateUIButton(detail.rectTransform, "CreditsButton", new Vector2(0f, -12f), new Vector2(40, 9f));

            mPlay.SetButtonText("Play with Lobby");
            mPlay.SetButtonTextSize(5f);
            mPlay.gameObject.SetActive(false);
            mPlay.ToggleWordWrapping(false);
            mPlay.onClick.AddListener(didSelectPlay);
            
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

        private void toggleButtons(bool val)
        {
            try
            {
                if (Data.Steam.SteamAPI.GetConnectionState() != SteamAPI.ConnectionState.CONNECTED || (!_partyFlowCoordinator || !_partyFlowCoordinator.isActivated))
                {
                    mPlay.gameObject.SetActive(false);
                    return;
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
                if (mPlay && !Controllers.PlayerController.Instance.AllPlayersInMenu())
                {
                    mPlay.SetButtonText("Players still in song");
                    mPlay.interactable = false;
                }
            } catch(Exception e)
            {
                Data.Logger.Error(e);
            }
        }

        private void didSelectPlay()
        {try
            {
                if (!_partyFlowCoordinator || !_partyFlowCoordinator.isActivated)
                {
                    toggleButtons(true);
                    return;
                }
                toggleButtons(false);
                if (Controllers.PlayerController.Instance.AllPlayersInMenu())
                {
                    SteamAPI.RequestPlay(new GameplayModifiers(_gameplaySetupViewController.gameplayModifiers));
                }
            } catch (Exception e)
            {
                Logger.Error(e);
            }
        }

        private void didSelectBeatmap(BeatmapDifficultyViewController controller, IDifficultyBeatmap beatmap)
        {
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
