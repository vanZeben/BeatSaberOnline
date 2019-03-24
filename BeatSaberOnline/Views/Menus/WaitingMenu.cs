using CustomUI.BeatSaber;
using System.Collections.Generic;
using UnityEngine;
using SteamAPI = BeatSaberOnline.Data.Steam.SteamAPI;
using BeatSaberOnline.Data;
using Logger = BeatSaberOnline.Data.Logger;
using HMUI;
using System;
using BeatSaberOnline.Views.ViewControllers;
using System.Linq;
using BeatSaberOnline.Utils;
using SongLoaderPlugin;
using SongLoaderPlugin.OverrideClasses;

namespace BeatSaberOnline.Views.Menus
{
    class WaitingMenu
    {
        static Vector2 BASE = new Vector2(-40f, 32.5f);
        public static CustomMenu Instance = null;
        private static ListViewController middleViewController;
        public static TMPro.TextMeshProUGUI level = null;
        public static bool firstInit = true;
        private static SongPreviewPlayer _songPreviewPlayer;
        public static BeatmapLevelSO queuedSong;
        public static bool downloading = false;
        public static bool autoReady = false;
        public static float timeRequestedToLaunch = 0f;

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

                Instance.SetMainViewController(middleViewController, true, (firstActivation, type) =>
                {
                    try
                    {
                        if (firstActivation)
                        {
                            level = middleViewController.CreateText("", new Vector2(BASE.x + 40f, BASE.y));
                            level.alignment = TMPro.TextAlignmentOptions.Center;

                            SteamAPI.SendPlayerPacket(Controllers.PlayerController.Instance._playerInfo);
                        }
                        if (firstInit)
                        {
                            firstInit = false;
                            RefreshData();
                        }
                    } catch (Exception e)
                    {
                        Logger.Error(e);
                    }
                });

            }
        }

       
        private static void ReadyUp(BeatmapLevelSO song)
        {
            if (queuedSong != null || (queuedSong == null && song == null)) { return; }
            if (queuedSong == null && song != null)
            {
                queuedSong = song;
                if (autoReady)
                {
                    PracticeSettings pSettings = null;
                    if (SteamAPI.GetSongOffset() > 0f)
                    {
                        pSettings = new PracticeSettings();
                        float offsetTime = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds() - timeRequestedToLaunch;
                        // add 5 seconds to it because that's about how long it takes to launch a song
                        pSettings.startSongTime = SteamAPI.GetSongOffset() + offsetTime + 5f;
                        timeRequestedToLaunch = 0;
                    }

                    SongListUtils.StartSong(song, SteamAPI.GetSongDifficulty(), SteamAPI.GetGameplayModifiers(), pSettings);
                }
                else
                {
                    SteamAPI.SetReady();
                    PreviewPlayer.CrossfadeTo(song.previewAudioClip, song.previewStartTime, song.previewDuration);
                }
                
            }
        }
        public static void RefreshData(BeatmapLevelSO song = null)
        {
            try
            {
                Logger.Debug("Refresh waiting menu data");
                if (queuedSong == null)
                {
                    if (song == null)
                    {
                        song = SongListUtils.GetInstalledSong();
                    }
                    Logger.Debug($"Refresh Waiting Menu data - Song is {(song != null ? "not" : "")} loaded");
                    if (song != null)
                    {

                        level.text = $"Queued: { song.songName} by { song.songAuthorName }";
                        if (song is CustomLevel)
                        {
                            SongLoader.Instance.LoadAudioClipForLevel((CustomLevel)song, (customLevel) =>
                            {
                                Logger.Debug($"Loaded audio Clip for {song.songName}");
                                ReadyUp(customLevel);
                            });
                        }
                        else
                        {
                            ReadyUp(song);
                        }
                    }
                    else if (!downloading)
                    {
                        level.text = $"Downloading: { SteamAPI.GetSongName()}";

                        Logger.Debug($"We do not have the song in our library, lets start downloading it.");
                        downloading = true;
                        try
                        {
                            Utils.SongDownloader.Instance.StartCoroutine(Utils.SongDownloader.Instance.DownloadSong(SteamAPI.GetSongId(), LevelDownloadProgress, LevelDownloaded, LevelError));
                        } catch (Exception e)
                        {
                            LevelError(e.Message);
                            Logger.Error(e);
                        }
                    }
                }
                Dictionary<string, float> status = Controllers.PlayerController.Instance.GetConnectedPlayerDownloadStatus();
                middleViewController.Data.Clear();
                foreach (KeyValuePair<string, float> user in status.OrderBy(u => u.Value))
                {
                    Logger.Debug($"{user.Key}: {user.Value}");
                    CustomCellInfo cell = new CustomCellInfo(user.Key, user.Value == -1f ? "FAILED TO DOWNLOAD": user.Value == 1f ? "Ready" : $"Downloading song {(int) Math.Round(user.Value * 100, 0)}%", user.Value == 1f ? Sprites.checkmarkIcon : Sprites.crossIcon);
                    middleViewController.Data.Add(cell);
                }
                middleViewController._customListTableView.ReloadData();
                middleViewController._customListTableView.ScrollToCellWithIdx(0, TableView.ScrollPositionType.Beginning, false);
            }
            catch (Exception e)
            {
                Logger.Debug($"Exception: {e}");
                Logger.Error(e);
            }
        }

        public static void LevelDownloadProgress(float progress)
        {
            Controllers.PlayerController.Instance._playerInfo.playerProgress = progress;
            if (progress == -1)
            {
                downloading = false;
            }
            else
            {
                RefreshData(null);
            }
        }

        public static void LevelError(string error)
        {
            Logger.Error($"Error downloading song: {error}");
            downloading = false;
            RefreshData(null);
        }

        public static void LevelDownloaded(string hash)
        {
            try
            {
                downloading = false;
                BeatmapLevelSO song = SongListUtils.GetInstalledSong(hash.ToUpper());
                RefreshData(song);
            } catch (Exception e)
            {
                Logger.Error(e);
            }
        }
    }
}
