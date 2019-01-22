using IllusionPlugin;
using UnityEngine.SceneManagement;
using BeatSaberOnline.Data;
using BeatSaberOnline.Views;

namespace BeatSaberOnline
{
    public class Plugin : IPlugin
    {
        public static Plugin instance;
        public string Name => "BeatSaberOnline";
        public string Version => "0.0.1";

        public void OnApplicationStart()
        {
            Init();
        }

        private void Init()
        {
            instance = this;
            Logger.Init();
            Config.Init();

            SceneManager.activeSceneChanged += ActiveSceneChanged;
        }

        public void OnApplicationQuit()
        {
            SceneManager.activeSceneChanged -= ActiveSceneChanged;
        }

        public void OnLevelWasLoaded(int level)
        {

        }

        public void OnLevelWasInitialized(int level)
        {
        }

        public void OnUpdate()
        {
        }

        public void OnFixedUpdate()
        {
        }


        private void ActiveSceneChanged(Scene from, Scene to)
        {
            Logger.Debug($"Active scene changed from \"{from.name}\" to \"{to.name}\"");
            if (from.name == "EmptyTransition" && to.name == "Menu")
            {
                PluginUI.Init();
            }
        }
    }
}
