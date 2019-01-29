
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

        public static void MoveFilesRecursively(DirectoryInfo source, DirectoryInfo target)
        {
            foreach (DirectoryInfo dir in source.GetDirectories())
                MoveFilesRecursively(dir, target.CreateSubdirectory(dir.Name));
            foreach (FileInfo file in source.GetFiles())
                file.MoveTo(Path.Combine(target.FullName, file.Name));
        }

        public static void EmptyDirectory(string directory, bool delete = true)
        {
            if (Directory.Exists(directory))
            {
                var directoryInfo = new DirectoryInfo(directory);
                foreach (System.IO.FileInfo file in directoryInfo.GetFiles()) file.Delete();
                foreach (System.IO.DirectoryInfo subDirectory in directoryInfo.GetDirectories()) subDirectory.Delete(true);

                if (delete) Directory.Delete(directory);
            }
        }

        public static IEnumerator ExtractZip(string zipPath, string extractPath)
        {
            if (File.Exists(zipPath))
            {
                bool extracted = false;
                try
                {
                    ZipFile.ExtractToDirectory(zipPath, ".mpdownloadcache");
                    extracted = true;
                }
                catch (Exception)
                {
                    Data.Logger.Info($"An error occured while trying to extract \"{zipPath}\"!");
                    yield break;
                }

                yield return new WaitForSeconds(0.25f);

                File.Delete(zipPath);

                try
                {
                    if (extracted)
                    {
                        if (!Directory.Exists(extractPath))
                            Directory.CreateDirectory(extractPath);

                        MoveFilesRecursively(new DirectoryInfo($"{Environment.CurrentDirectory}\\.mpdownloadcache"), new DirectoryInfo(extractPath));
                    }
                }
                catch (Exception e)
                {
                    Data.Logger.Info($"An exception occured while trying to move files into their final directory! {e.ToString()}");
                }
            }
        }

        public static IEnumerator DownloadFile(string url, string path)
        {
            using (UnityWebRequest www = UnityWebRequest.Get(url))
            {
                yield return www.SendWebRequest();

                if (www.isNetworkError || www.isHttpError)
                {
                    Data.Logger.Error($"Http request error! {www.error}");
                    yield break;
                }
                Data.Logger.Debug($"Success downloading \"{url}\"");
                byte[] data = www.downloadHandler.data;
                try
                {
                    if (!Directory.Exists(Path.GetDirectoryName(path)))
                        Directory.CreateDirectory(Path.GetDirectoryName(path));

                    File.WriteAllBytes(path, data);
                    Data.Logger.Debug("Downloaded file!");
                }
                catch (Exception)
                {
                    Data.Logger.Error("Failed to download file!");
                    yield break;
                }
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
                    EmptyDirectory(".mpdownloadcache");

                    string zipPath = Path.Combine(Environment.CurrentDirectory, ".mpdownloadcache", $"{song["version"].Value}.zip");
                    string finalPath = Path.Combine(Environment.CurrentDirectory, "CustomSongs", song["version"].Value);

                    if (Directory.Exists(finalPath))
                        Directory.Delete(finalPath, true);

                    Data.Logger.Debug($"ZipPath: {zipPath}");
                    yield return DownloadFile(song["downloadUrl"].Value, zipPath);
                    yield return ExtractZip(zipPath, finalPath);

                    SongLoader.Instance.RefreshSongs(false);
                    while (SongLoader.AreSongsLoading) yield return null;
                    EmptyDirectory(".mpdownloadcache", true);

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