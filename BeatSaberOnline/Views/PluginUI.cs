using BeatSaberOnline.Data;
using CustomUI.Settings;
using System;
using UnityEngine;
using Logger = BeatSaberOnline.Data.Logger;

namespace BeatSaberOnline.Views
{
    class PluginUI : MonoBehaviour
    {
        public static PluginUI instance;

        public static void Init()
        {
            if (instance != null)
            {
                instance.CreateUI();
                return;
            }
            new GameObject(Plugin.instance.Name).AddComponent<PluginUI>();
        }

        public void Awake()
        {
            if (instance != this)
            {
                instance = this;
                CreateUI();
            }
        }

        protected void CreateUI()
        {
            try
            {
                CreateSettingsMenu();
            }
            catch (Exception e)
            {
                Logger.Error($"Unable to create UI! Exception: {e}");
            }
        }

        private void CreateSettingsMenu()
        {
            var settingsMenu = SettingsUI.CreateSubMenu(Plugin.instance.Name);

            var doTheThing = settingsMenu.AddBool("Do the thing");
            doTheThing.GetValue += delegate { return Config.Instance.DoTheThing; };
            doTheThing.SetValue += delegate (bool value) { Config.Instance.DoTheThing = value; };

        }
    }
}
