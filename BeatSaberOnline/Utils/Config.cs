using System;
using System.IO;

namespace BeatSaberOnline.Data
{
     public class Config
     {
        public static Config Instance;

        public string FilePath { get; }
        public bool autoStartLobby = false;
        public bool isPublic = true;
        public int maxLobbySize = 5;
        public bool avatarsInLobby = true;
        public bool avatarsInGame = true;
        public int networkQuality = 5;
        public float volume = 20;

        public bool AutoStartLobby { get { return autoStartLobby; } set { autoStartLobby = value; Save(); } }
        public bool IsPublic { get { return isPublic; } set { isPublic = value; Save(); } }
        public int MaxLobbySize { get { return maxLobbySize; } set { maxLobbySize = value; Save(); } }
        public bool AvatarsInLobby { get { return avatarsInLobby; } set { avatarsInLobby = value; Save(); } }
        public bool AvatarsInGame { get { return avatarsInGame; } set { avatarsInGame = value; Save(); } }
        public int NetworkQuality { get { return networkQuality; } set { networkQuality = value; Save(); } }
        public float Volume { get { return volume; } set { volume = value; Save(); } }

        public event Action<Config> ConfigChangedEvent;
        private readonly FileSystemWatcher _configWatcher;
        private bool _saving;

        public Config(string filePath)
        {
            Instance = this;
            FilePath = filePath;

            if (!Directory.Exists(Path.GetDirectoryName(FilePath)))
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
            if (File.Exists(Path.Combine(Environment.CurrentDirectory, "UserData/BeatSaberOnline.json")))
                File.Delete(Path.Combine(Environment.CurrentDirectory, "UserData/BeatSaberOnline.json"));

            if (File.Exists(FilePath))
            {
                Load();
            }
            Save();
            _configWatcher = new FileSystemWatcher(Path.Combine(Environment.CurrentDirectory, "UserData"))
            {
                NotifyFilter = NotifyFilters.LastWrite,
                Filter = "BeatSaberOnline.ini",
                EnableRaisingEvents = true
            };
            _configWatcher.Changed += ConfigWatcherOnChanged;
        }

        ~Config()
        {
            _configWatcher.Changed -= ConfigWatcherOnChanged;
        }

        public void Load()
        {
            ConfigSerializer.LoadConfig(this, FilePath);

            CorrectConfigSettings();
        }

        private void CorrectConfigSettings()
        {
            if (volume > 20)
            {
                volume = 20;
            }
            else if (volume < 0)
            {
                volume = 0;
            }
            if (networkQuality > 5)
            {
                networkQuality = 5;
            } else if (networkQuality < 0)
            {
                networkQuality = 0;
            }
        }

        public void Save(bool callback = false)
        {
            if (!callback)
                _saving = true;
            ConfigSerializer.SaveConfig(this, FilePath);
        }
        private void ConfigWatcherOnChanged(object sender, FileSystemEventArgs fileSystemEventArgs)
        {
            if (_saving)
            {
                _saving = false;
                return;
            }

            Load();
            ConfigChangedEvent?.Invoke(this);
        }
    }
}