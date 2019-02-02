using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace BeatSaberOnline.Utils
{
    public class FileUtils
    {

        public static void MoveFilesRecursively(DirectoryInfo source, DirectoryInfo target, bool moveOld)
        {
            try
            {
                foreach (DirectoryInfo dir in source.GetDirectories())
                {
                    MoveFilesRecursively(dir, target.CreateSubdirectory(dir.Name), moveOld);
                }
                foreach (FileInfo file in source.GetFiles())
                {
                    if (moveOld && File.Exists(Path.Combine(target.FullName, file.Name)))
                    {
                        File.Move(Path.Combine(target.FullName, file.Name), Path.Combine(target.FullName, $"{file.Name}.old"));
                    }
                    file.MoveTo(Path.Combine(target.FullName, file.Name));
                }
            }
            catch (Exception e)
            {
                Data.Logger.Error(e);
            }
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

        public static IEnumerator ExtractZip(string zipPath, string extractPath, string cachePath, bool moveOld)
        {
            if (File.Exists(zipPath))
            {
                bool extracted = false;
                try
                {
                    ZipFile.ExtractToDirectory(zipPath, $"{cachePath}");
                    extracted = true;
                }
                catch (Exception e)
                {
                    Data.Logger.Debug($"An error occured while trying to extract \"{zipPath}\"!");
                    Data.Logger.Error(e);
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

                        MoveFilesRecursively(new DirectoryInfo($"{Environment.CurrentDirectory}\\{ cachePath }"), new DirectoryInfo(extractPath), moveOld);
                    }
                }
                catch (Exception e)
                {
                    Data.Logger.Debug($"An exception occured while trying to move files into their final directory! {e.ToString()}");
                    Data.Logger.Error(e);
                }
            }
        }

        public static IEnumerator DownloadFile(string url, string path, Action<float> progressChanged)
        {
            using (UnityWebRequest www = UnityWebRequest.Get(url))
            {
                float initTime = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds();
                UnityWebRequestAsyncOperation req = www.SendWebRequest();

                while (!req.isDone)
                {
                    yield return null;
                    
                    if (new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds() - initTime > 5 && req.progress == 0f)
                    {
                        Data.Logger.Error("Did not download anything within 5 second, aborting download");
                        www.Abort();
                        yield break;

                    }
                    progressChanged?.Invoke(req.progress);
                }
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
    }
}
