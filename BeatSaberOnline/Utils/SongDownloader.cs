
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


        public IEnumerator DownloadSong(string levelId, Action<string> songDownloaded)
        {
            levelId = levelId.Substring(0, 32);

            using (UnityWebRequest www = UnityWebRequest.Get($"https://beatsaver.com/api/songs/search/hash/{levelId}"))
            {
                yield return www.SendWebRequest();
                if (www.isNetworkError || www.isHttpError)
                {
                    Data.Logger.Error(www.error);
                    yield break;
                }

                JSONNode result = JSON.Parse(www.downloadHandler.text);
                if (result["total"].AsInt == 0) yield break;

                foreach (JSONObject song in result["songs"].AsArray)
                {
                    FileUtils.EmptyDirectory(".mpdownloadcache");

                    string zipPath = Path.Combine(Environment.CurrentDirectory, ".mpdownloadcache", $"{song["version"].Value}.zip");
                    string finalPath = Path.Combine(Environment.CurrentDirectory, "CustomSongs", song["version"].Value);

                    if (Directory.Exists(finalPath))
                        Directory.Delete(finalPath, true);

                    Data.Logger.Debug($"ZipPath: {zipPath}");
                    yield return FileUtils.DownloadFile(song["downloadUrl"].Value, zipPath);
                    yield return FileUtils.ExtractZip(zipPath, finalPath, ".mpdownloadcache", false);

                    SongLoader.Instance.RefreshSongs(false);
                    while (SongLoader.AreSongsLoading) yield return null;
                    FileUtils.EmptyDirectory(".mpdownloadcache", true);

                    songDownloaded?.Invoke(song["hashMd5"]);
                    break;
                }
            }
        }

        public void downloadedSong(Song song)
        {
            CurrentlyDownloadingSong = null;
        }
        
    }
}