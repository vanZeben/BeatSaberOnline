
using BeatSaverDownloader.Misc;
using SimpleJSON;
using SongLoaderPlugin;
using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using Logger = BeatSaberOnline.Data.Logger;

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

        public static IEnumerator CheckSongExists(string levelId, Action<bool> hasSong)
        {
            levelId = levelId.Substring(0, 32);
            using (UnityWebRequest www = UnityWebRequest.Get($"https://beatsaver.com/api/songs/search/hash/{levelId}"))
            {
                yield return www.SendWebRequest();
                if (www.isNetworkError || www.isHttpError)
                {
                    Data.Logger.Error(www.error);
                    hasSong?.Invoke(false);
                    yield break;
                }

                JSONNode result = JSON.Parse(www.downloadHandler.text);
                hasSong?.Invoke(result["total"].AsInt > 0);
            }
        }

        public IEnumerator DownloadSong(string levelId, Action<float> downloadProgress, Action<string> songDownloaded, Action<string> downloadError)
        {
            levelId = levelId.Substring(0, 32);
            Data.Logger.Info($"Starting download for {levelId}");
            using (UnityWebRequest www = UnityWebRequest.Get($"https://beatsaver.com/api/songs/search/hash/{levelId}"))
            {
                yield return www.SendWebRequest();
                if (www.isNetworkError || www.isHttpError)
                {
                    Logger.Error(www.error);
                    downloadError?.Invoke(www.error);
                    yield break;
                }

                JSONNode result = JSON.Parse(www.downloadHandler.text);
                Logger.Debug($"Result: {result}");
                if (result["total"].AsInt == 0)
                {
                    downloadError?.Invoke("song not found");
                    yield break;
                }
                foreach (JSONObject song in result["songs"].AsArray)
                {
                    FileUtils.EmptyDirectory(".mpdownloadcache");

                    string zipPath = Path.Combine(Environment.CurrentDirectory, ".mpdownloadcache", $"{song["version"].Value}.zip");
                    string finalPath = Path.Combine(Environment.CurrentDirectory, "CustomSongs", Plugin.instance.Name, song["version"].Value);

                    if (Directory.Exists(finalPath))
                        Directory.Delete(finalPath, true);

                    yield return FileUtils.DownloadFile(song["downloadUrl"].Value, zipPath, downloadProgress);
                    yield return FileUtils.ExtractZip(zipPath, finalPath, ".mpdownloadcache", false);

                    SongLoader.Instance.RefreshSongs(false);
                    float initTime = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds();

                    while (SongLoader.AreSongsLoading)
                    {
                        yield return null;
                        if (initTime - new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds() > 5)
                        {
                            downloadError?.Invoke("timeout");
                            yield break;
                        }
                    }
                    FileUtils.EmptyDirectory(".mpdownloadcache", true);
                    songDownloaded?.Invoke(song["hashMd5"]);
                    break;
                }
            }
        }
    }
}