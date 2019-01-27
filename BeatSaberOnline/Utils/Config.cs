using System;
using System.IO;
using UnityEngine;

namespace BeatSaberOnline.Data
{
    [Serializable]
    public class Config
    {

        [SerializeField] private bool _autoStartLobby;
        [SerializeField] private bool _isPublic;
        [SerializeField] private int _maxLobbySize;
        [SerializeField] private bool _noFailMode;
        [SerializeField] private bool _debugMode;

        private static Config _instance;

        private static FileInfo FileLocation { get; } = new FileInfo($"./UserData/{Plugin.instance.Name}.json");

        public static void Init()
        {
            if (!Load())
                Create();
        }

        public static void Reload()
        {
            if (_instance.IsDirty)
                _instance.Save();

            _instance = null;
            Load();
        }

        private static bool Load()
        {
            if (_instance != null) return false;
            try
            {
                FileLocation?.Directory?.Create();
                _instance = JsonUtility.FromJson<Config>(File.ReadAllText(FileLocation.FullName));
                _instance.MarkDirty();
                _instance.Save();
            }
            catch (Exception)
            {
                Logger.Error($"Unable to load config @ {FileLocation.FullName}");
                return false;
            }
            return true;
        }

        private static bool Create()
        {
            if (_instance != null) return false;
            try
            {
                FileLocation?.Directory?.Create();
                Instance.Save();
            }
            catch (Exception)
            {
                Logger.Error($"Unable to create new config @ {FileLocation.FullName}");
                return false;
            }
            return true;
        }

        public static Config Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new Config();
                return _instance;
            }
        }

        private bool IsDirty { get; set; }

        public bool AutoStartLobby
        {
            get { return _autoStartLobby; }
            set
            {
                _autoStartLobby = value;
                MarkDirty();
            }
        }

        public bool IsPublic
        {
            get { return _isPublic; }
            set
            {
                _isPublic = value;
                MarkDirty();
            }
        }

        public int MaxLobbySize
        {
            get { return _maxLobbySize; }
            set
            {
                _maxLobbySize = value;
                MarkDirty();
            }
        }
        public bool NoFailMode
        {
            get { return _noFailMode; }
            set
            {
                _noFailMode = value;
                MarkDirty();
            }
        }
        public bool DebugMode
        {
            get { return _debugMode; }
            set
            {
                _debugMode = value;
                MarkDirty();
            }
        }


        Config()
        {
            _autoStartLobby = false;
            _isPublic = true;
            _maxLobbySize = 5;
            _noFailMode = true;
            _debugMode = false;
            IsDirty = true;
        }

        public bool Save()
        {
            if (!IsDirty) return false;
            try
            {
                using (var f = new StreamWriter(FileLocation.FullName))
                {
                    var json = JsonUtility.ToJson(this, true);
                    f.Write(json);
                }
                MarkClean();
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error($"Unable to write the config file! Exception: {ex}");
                return false;
            }
        }

        void MarkDirty()
        {
            IsDirty = true;
            Save();
        }

        void MarkClean()
        {
            IsDirty = false;
        }
    }
}