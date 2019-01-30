using BeatSaberOnline.Views.Menus;
using CustomUI.BeatSaber;
using SimpleJSON;
using System;
using System.Collections;
using System.IO;
using UnityEngine.Networking;

namespace BeatSaberOnline.Utils
{
    public class AutoUpdater
    {
        private static string _link = "https://www.modsaber.org/api/v1.1/mods/versions/beat-saber-online";
       

        public static IEnumerator GetLatestVersionDownload()
        {
                if (File.Exists($"Plugins/{Plugin.instance.Name}.dll.old"))
                {
                    File.Delete($"Plugins/{Plugin.instance.Name}.dll.old");
                }
                using (UnityWebRequest www = UnityWebRequest.Get(_link))
                {
                    yield return www.SendWebRequest();
                    if (www.isNetworkError || www.isHttpError)
                    {
                        Data.Logger.Error(www.error);
                        yield break;
                    }

                    JSONNode result = JSON.Parse(www.downloadHandler.text);
                    if (result.Count == 0) yield break;
                    string version = result[0]["version"].Value;
                    int latestVersion = Convert.ToInt32(version.Replace(".", ""));
                    int currentVersion = Convert.ToInt32(Plugin.instance.Version.Replace(".", ""));
                    string status = result[0]["approval"];

                    // Check if the remote version on modsaber is newer than what we have and if that version is approved
                    if (latestVersion <= currentVersion && status == "approved") yield break;
              
                    Data.Logger.Info($"Found a new version, lets download it {result[0]["files"]["steam"]["url"].Value}");
                
                    yield break;
                    
                    // Run the actual download
                    // Talked to lolPants about this and he said he would talk to the approval team first to come back with whether or not we should do this so for now lets just break and not run the actual download until this functionality is approved

                    FileUtils.EmptyDirectory(".pluginupdatecache");
                
                    string zipPath = Path.Combine(Environment.CurrentDirectory, ".pluginupdatecache", $"{Plugin.instance.Name}.zip");
                    string finalPath = Path.Combine(Environment.CurrentDirectory);
                
                    yield return FileUtils.DownloadFile(result[0]["files"]["steam"]["url"].Value, zipPath);
                    yield return FileUtils.ExtractZip(zipPath, finalPath, ".pluginupdatecache", true);

                    FileUtils.EmptyDirectory(".pluginupdatecache");
                    Data.Logger.Debug($"Updated {Plugin.instance.Name} to version {version}. Please restart your game now");

                    Plugin.instance.UpdatedVersion = version;
                    AutoUpdateMenu.Init();
            }
        }
    }
}
