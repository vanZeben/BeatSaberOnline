using CustomUI.BeatSaber;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using SteamAPI = BeatSaberOnline.Data.Steam.SteamAPI;
using Steamworks;
using BeatSaberOnline.Data;
using Logger = BeatSaberOnline.Data.Logger;
using CustomUI.Utilities;
using HMUI;
using System;
using BeatSaberOnline.Views.ViewControllers;
using static VRUI.VRUIViewController;
using BeatSaberOnline.Controllers;
using System.Text;
using System.Linq;
using VRUI;
using System.Reflection;
using BeatSaberOnline.Utils;
using SongLoaderPlugin;
using SongLoaderPlugin.OverrideClasses;
using BeatSaverDownloader.Misc;

namespace BeatSaberOnline.Views.Menus
{
    class WaitingMenu
    {
        static Vector2 BASE = new Vector2(-40f, 32.5f);
        public static CustomMenu Instance = null;
        private static ListViewController middleViewController;
        public static TMPro.TextMeshProUGUI level = null;
        public static bool firstInit = true;
        private static Song downloadingSong;
        private static SongPreviewPlayer _songPreviewPlayer;

        public static SongPreviewPlayer PreviewPlayer
        {
            get
            {
                if (_songPreviewPlayer == null)
                {
                    _songPreviewPlayer = Resources.FindObjectsOfTypeAll<SongPreviewPlayer>().FirstOrDefault();
                }

                return _songPreviewPlayer;
            }
            private set { _songPreviewPlayer = value; }
        }
    
    public static void Init()
        {
            if (Instance == null)
            {
                Instance = BeatSaberUI.CreateCustomMenu<CustomMenu>("Waiting for players");
                middleViewController = BeatSaberUI.CreateViewController<ListViewController>();
                Instance.SetMainViewController(middleViewController, false, (firstActivation, type) =>
                {
                    try
                    {
                        if (firstActivation)
                        {
                            level = middleViewController.CreateText("", new Vector2(BASE.x + 40f, BASE.y));
                            level.alignment = TMPro.TextAlignmentOptions.Center;

                            Controllers.PlayerController.Instance.UpdatePlayerScoring("playerScore", 0);
                            SteamAPI.SendPlayerInfo(Controllers.PlayerController.Instance._playerInfo);
                        }
                        if (firstInit)
                        {
                            firstInit = false;
                            RefreshData(true);
                        }
                    } catch (Exception e)
                    {
                        Logger.Error(e);
                    }
                });

            }
        }

        private static void downloadedSong(Song song)
        {
            Logger.Info("Finished downloading " + song.songName);
        }
        public static void RefreshData(bool ready)
        {
            try
            {
                LevelSO song = GetInstalledSong();

                if (song != null)
                {
                    level.text = $"Queued: { song.songName} by { song.songAuthorName }";
                    if (ready) { SteamAPI.SetReady(); }
                    PreviewPlayer.CrossfadeTo(song.audioClip, song.previewStartTime, song.previewDuration);
                } else {
                   Instance.StartCoroutine(Utils.SongDownloader.Instance.DownloadSong(SteamAPI.GetSongId(), () => { RefreshData(true); }));
                }

                if (Instance && Instance.isActiveAndEnabled)
                {
                    var status = SteamAPI.getAllPlayerStatusesInLobby();
                    middleViewController.Data.Clear();
                    bool allReady = true;
                    foreach (KeyValuePair<string, bool> user in status.OrderBy(u => u.Value))
                    {
                        CustomCellInfo cell = new CustomCellInfo(user.Key,  user.Value ? "Ready" : "Downloading song", user.Value ? Sprites.checkmarkIcon : Sprites.crossIcon);
                        middleViewController.Data.Add(cell);
                        if (!user.Value)
                        {
                            allReady = false;
                        }
                    }
                    middleViewController._customListTableView.ReloadData();
                    middleViewController._customListTableView.ScrollToRow(0, false);
                    if (allReady)
                    {
                        if (song is CustomLevel)
                        { 
                            SongLoader.Instance.LoadAudioClipForLevel((CustomLevel)song, (customLevel) =>
                            {
                                SongListUtils.StartSong(customLevel, SteamAPI.GetSongDifficulty());
                                SteamAPI.ResetScreen();
                            });
                        }
                        else
                        {
                            SongListUtils.StartSong(song, SteamAPI.GetSongDifficulty());
                            SteamAPI.ResetScreen();
                        }
                    }
                }
            } catch (Exception e)
            {
                Logger.Error(e);
            }
        }

        private static LevelSO GetInstalledSong()
        {
            string levelId = SteamAPI.GetSongId();
            LevelSO level;
            if (levelId.Length > 32)
            {
                if (SongLoader.CustomLevels == null) { return null;  }
                LevelSO[] levels = SongLoader.CustomLevels.Where(l => l.levelID.StartsWith(levelId.Substring(0, 32))).ToArray();
                level = levels.Length > 0 ? levels[0] : null;
            } else
            {
                if (SongLoader.CustomLevelCollectionSO.levels == null) { return null; }
                LevelSO[] levels = SongLoader.CustomLevelCollectionSO.levels.Where(l => l.levelID.StartsWith(levelId)).ToArray();
                level = levels.Length > 0 ? levels[0] : null;
            }
            return level;
        }
    }
}
