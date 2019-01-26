﻿using BeatSaberOnline.Data.Steam;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

using UnityEngine.XR;
namespace BeatSaberOnline.Controllers
{
    class LeaderboardController: MonoBehaviour
    {
        public static LeaderboardController Instance;
        private ScoreController _scoreController;
        private GameEnergyCounter _energyController;

        public static void Init(Scene to)
        {
            if (Instance != null)
            {
                return;
            }

            new GameObject("LeaderboardController").AddComponent<LeaderboardController>();
        }

        public void Awake()
        {
            if (Instance != this)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
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
                if (to.name == "GameCore")
                {
                    StartCoroutine(InitControllers());
                }
                else if (to.name == "Menu")
                {
                    StopCoroutine(InitControllers());
                }
            }
            catch (Exception e)
            {
                Data.Logger.Error($"(OnlineController) Exception on {to.name} scene activation! Exception: {e}");
            }
        }
        
        IEnumerator InitControllers()
        {
            yield return new WaitUntil(delegate () { return FindObjectOfType<ScoreController>() != null && FindObjectOfType<GameEnergyCounter>() != null; });

            _scoreController = FindObjectOfType<ScoreController>();
            _energyController = FindObjectOfType<GameEnergyCounter>();

            if (_scoreController != null)
            {
                _scoreController.scoreDidChangeEvent += ScoreChanged;
                _scoreController.noteWasCutEvent += NoteWasCutEvent;
                _scoreController.comboDidChangeEvent += ComboDidChangeEvent;
                _scoreController.noteWasMissedEvent += NoteWasMissedEvent;
            }
            
            if (_energyController != null)
            {
                _energyController.gameEnergyDidChangeEvent += EnergyDidChangeEvent;
            }
        }
        private void EnergyDidChangeEvent(float energy)
        {
            PlayerController.Instance.UpdatePlayerScoring("playerEnergy", (uint) Math.Round(energy * 100));
        }

        private void ComboDidChangeEvent(int obj)
        {
            PlayerController.Instance.UpdatePlayerScoring("playerComboBlocks", (uint) obj);
        }

        private void NoteWasCutEvent(NoteData note, NoteCutInfo cut, int score)
        {
            if (cut.allIsOK)
            {
                PlayerController.Instance.UpdatePlayerScoring("playerCutBlocks", 1);
            }
            PlayerController.Instance.UpdatePlayerScoring("playerTotalBlocks", 1);
        }

        private void NoteWasMissedEvent(NoteData note, int arg2)
        {
            PlayerController.Instance.UpdatePlayerScoring("playerTotalBlocks",  1);
        }

        private void ScoreChanged(int score)
        {
            PlayerController.Instance.UpdatePlayerScoring("playerScore", (uint) score);
        }
    }
}
