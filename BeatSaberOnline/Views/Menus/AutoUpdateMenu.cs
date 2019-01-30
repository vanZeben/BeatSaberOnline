using CustomUI.BeatSaber;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using SteamAPI = BeatSaberOnline.Data.Steam.SteamAPI;
using Steamworks;
using BeatSaberOnline.Data;
using Logger = BeatSaberOnline.Data.Logger;
using CustomUI.Utilities;
using HMUI;
using System;
using BeatSaberOnline.Views.ViewControllers;
using static VRUI.VRUIViewController;
using BeatSaberOnline.Controllers;
using System.Text;
using System.Linq;
using VRUI;
using System.Reflection;
using BeatSaberOnline.Data.Steam;

namespace BeatSaberOnline.Views.Menus
{
    class AutoUpdateMenu
    {
        static Vector2 BASE = new Vector2(-40f, 30f);
        public static CustomMenu Instance = null;
        public static void Init()
        {
            if (Instance == null)
            {
                Instance = BeatSaberUI.CreateCustomMenu<CustomMenu>($"{Plugin.instance.Name} has been updated");

                CustomViewController middleViewController = BeatSaberUI.CreateViewController<CustomViewController>();


                Instance.SetMainViewController(middleViewController, true, (firstActivation, type) =>
                {
                    if (firstActivation)
                    {
                        TMPro.TextMeshProUGUI t = middleViewController.CreateText($"{Plugin.instance.Name} has been updated to v{Plugin.instance.UpdatedVersion}. Restart your game to take effect.", new Vector2(0f, 0f));
                        t.alignment = TMPro.TextAlignmentOptions.Center;
                    }
                });
                Instance.Present();
                if (PluginUI.MultiplayerButton != null)
                {
                    PluginUI.MultiplayerButton.interactable = false;
                    PluginUI.MultiplayerButton.hintText = "You cannot access multiplayer until you restart your game";
                    for (int i = PluginUI.MultiplayerButton.buttons.Count - 1; i >= 0; i--)
                    {
                        BeatSaberUI.AddHintText(PluginUI.MultiplayerButton.buttons[i].transform as RectTransform, PluginUI.MultiplayerButton.hintText);
                    }
                }
            }
        }
    }
}
