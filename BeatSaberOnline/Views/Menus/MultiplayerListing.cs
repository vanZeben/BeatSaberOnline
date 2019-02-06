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
    class MultiplayerListing
    {
        static Vector2 BASE = new Vector2(-40f, 30f);
        public static CustomMenu Instance = null;
        private static Dictionary<CSteamID, LobbyPacket> lobbies;
        private static ListViewController middleViewController;
        private static bool sorting = false;
        private static Button refresh;
        private static Button sortingBtn;

        public static void Init()
        {
            if (Instance == null)
            {
                Instance = BeatSaberUI.CreateCustomMenu<CustomMenu>("Online Multiplayer");

                middleViewController = BeatSaberUI.CreateViewController<ListViewController>();


                Instance.SetMainViewController(middleViewController, true, (firstActivation, type) =>
                {
                    refreshAvailableLobbies();
                    if (firstActivation)
                    {
                        middleViewController.CreateText("Available Lobbies", new Vector2(BASE.x + 60f, BASE.y));

                        refresh = middleViewController.CreateUIButton("CreditsButton", new Vector2(BASE.x + 80f, BASE.y + 2.5f), new Vector2(25f, 7f));
                        refresh.SetButtonText("Refresh");
                        refresh.SetButtonTextSize(3f);
                        refresh.ToggleWordWrapping(false);
                        refresh.onClick.AddListener(delegate ()
                        {
                            refreshAvailableLobbies();
                        });
                        
                        if (!SteamAPI.isLobbyConnected())
                        {
                            Button host = middleViewController.CreateUIButton("CreditsButton", new Vector2(BASE.x, BASE.y + 2.5f), new Vector2(25f, 7f));
                            host.SetButtonTextSize(3f);
                            host.ToggleWordWrapping(false);

                            host.onClick.RemoveAllListeners();
                            host.SetButtonText("Host Public Lobby");
                            host.onClick.AddListener(delegate ()
                            {

                                SteamAPI.CreateLobby(false);
                                Instance.Dismiss();
                                MultiplayerLobby.Instance.Present();

                            });

                            Button hostP = middleViewController.CreateUIButton("CreditsButton", new Vector2(BASE.x, BASE.y + 2.5f - 10f), new Vector2(25f, 7f));
                            hostP.SetButtonTextSize(3f);
                            hostP.ToggleWordWrapping(false);

                            hostP.onClick.RemoveAllListeners();
                            hostP.SetButtonText("Host Private Lobby");
                            hostP.onClick.AddListener(delegate ()
                            {

                                SteamAPI.CreateLobby(true);
                                Instance.Dismiss();
                                MultiplayerLobby.Instance.Present();

                            });
                        }
                    }
                });

            }
        }
        
        
        private static void refreshAvailableLobbies()
        {
            lobbies = new Dictionary<CSteamID, LobbyPacket>();

            if (refresh) refresh.interactable = true;
            SteamAPI.RequestLobbies();
        }

        private static Dictionary<ulong, bool> availableLobbies = new Dictionary<ulong, bool>();
        public static void refreshLobbyList()
        {
            availableLobbies.Clear();
            middleViewController.Data.Clear();
            try
            {
                Dictionary<ulong, LobbyPacket> lobbies = SteamAPI.LobbyData;
                foreach (KeyValuePair<ulong, LobbyPacket> entry in lobbies)
                {
                    LobbyPacket info = SteamAPI.LobbyData[entry.Key];
                    availableLobbies.Add(entry.Key, info.Joinable);
                    middleViewController.Data.Add(new CustomCellInfo($"{(info.Joinable && info.TotalSlots - info.UsedSlots > 0 ? "":"[LOCKED]")}[{info.UsedSlots}/{info.TotalSlots}] {info.HostName}'s Lobby", $"{info.Status}"));
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
            middleViewController._customListTableView.ReloadData();
            middleViewController._customListTableView.ScrollToRow(0, false);
            middleViewController.DidSelectRowEvent = (TableView view, int row) =>
            {
                ulong clickedID = availableLobbies.Keys.ToArray()[row];
                LobbyPacket info = SteamAPI.LobbyData[clickedID];
                if (clickedID != 0 && availableLobbies.Values.ToArray()[row] && info.TotalSlots - info.UsedSlots > 0)
                {
                    Scoreboard.Instance.UpsertScoreboardEntry(Controllers.PlayerController.Instance._playerInfo.playerId, Controllers.PlayerController.Instance._playerInfo.playerName);
                    Instance.Dismiss();
                    MultiplayerLobby.Instance.Present();
                    SteamAPI.JoinLobby(new CSteamID(clickedID));
                }
            };
            if (refresh) refresh.interactable = true;
        }
    }
}
