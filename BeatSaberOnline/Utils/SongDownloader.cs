
using BeatSaverDownloader.Misc;
using SimpleJSON;
using SongLoaderPlugin;
using SongLoaderPlugin.OverrideClasses;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace BeatSaberOnline.Utils
{

    public class SongDownloader : MonoBehaviour
    {
        public static string BEATSAVER_URL = "https://beatsaver.com";
        public Song CurrentlyDownloadingSong { get; set; }

        private static SongDownloader _instance = null;
        public static SongDownloader Instance
        {
            get
            {
                if (!_instance)
                    _instance = new GameObject("SongDownloader").AddComponent<SongDownloader>();
                return _instance;
            }
            private set
            {
                _instance = value;
            }
        }
        

        public void DownloadSong(string levelId)
        {
            try
            {
                if (levelId.Length > 32) { levelId = levelId.Substring(0, 32); }
                BeatSaverDownloader.Misc.SongDownloader.Instance.songDownloaded += downloadedSong;
                BeatSaverDownloader.Misc.SongDownloader.Instance.RequestSongByLevelID(levelId, (song) =>
                {
                    CurrentlyDownloadingSong = song;
                    if (!BeatSaverDownloader.Misc.SongDownloader.Instance.IsSongDownloaded(CurrentlyDownloadingSong))
                    {
                        Data.Logger.Info("Starting to download " + levelId);
                        StartCoroutine(BeatSaverDownloader.Misc.SongDownloader.Instance.DownloadSongCoroutine(CurrentlyDownloadingSong));

                    }
                    else
                    {
                        Data.Logger.Info("Song is already downloaded");
                    }
                });
            } catch (Exception e)
            {
                Data.Logger.Error(e);
            }
        }

        public void downloadedSong(Song song)
        {
            CurrentlyDownloadingSong = null;
        }
        
    }
}