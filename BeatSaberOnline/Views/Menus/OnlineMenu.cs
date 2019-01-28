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
    class OnlineMenu
    {
        static Vector2 BASE = new Vector2(-40f, 30f);
        public static CustomMenu Instance = null;
        private static Dictionary<CSteamID, string[]> friends;
        private static Dictionary<CSteamID, LobbyInfo> lobbies;
        private static ListViewController middleViewController;
        private static ListViewController rightViewController;
        private static bool sorting = true;
        private static Button refresh;
        private static Button sortingBtn;
        public static void Init()
        {
            if (Instance == null)
            {
                Instance = BeatSaberUI.CreateCustomMenu<CustomMenu>("Online Multiplayer");

                middleViewController = BeatSaberUI.CreateViewController<ListViewController>();
                rightViewController = BeatSaberUI.CreateViewController<ListViewController>();


                Instance.SetMainViewController(middleViewController, true, (firstActivation, type) =>
                {
                    if (firstActivation)
                    {
                        middleViewController.CreateText("Available Lobbies", new Vector2(BASE.x + 60f, BASE.y));
                        refreshAvailableLobbies();

                        refresh = middleViewController.CreateUIButton("CreditsButton", new Vector2(BASE.x + 80f, BASE.y + 2.5f - 10f), new Vector2(25f, 7f));
                        refresh.SetButtonText("Refresh");
                        refresh.SetButtonTextSize(3f);
                        refresh.ToggleWordWrapping(false);
                        refresh.onClick.AddListener(delegate ()
                        {
                            refreshAvailableLobbies();
                        });

                        sortingBtn = middleViewController.CreateUIButton("CreditsButton", new Vector2(BASE.x + 80f, BASE.y + 2.5f), new Vector2(25f, 7f));
                        sortingBtn.SetButtonText(sorting ? "Friends Only" : "Public");
                        sortingBtn.SetButtonTextSize(3f);
                        sortingBtn.ToggleWordWrapping(false);
                        sortingBtn.onClick.AddListener(delegate ()
                        {
                            sorting = !sorting;
                            sortingBtn.SetButtonText(sorting ? "Friends Only" : "Public");

                            refreshAvailableLobbies();
                        });
                        if (!SteamAPI.isLobbyConnected())
                        {
                            Button host = middleViewController.CreateUIButton("CreditsButton", new Vector2(BASE.x, BASE.y + 2.5f), new Vector2(25f, 7f));
                            host.SetButtonTextSize(3f);
                            host.ToggleWordWrapping(false);

                            host.onClick.RemoveAllListeners();
                            host.SetButtonText("Host Lobby");
                            host.onClick.AddListener(delegate ()
                            {
                                SteamAPI.CreateLobby();
                                Instance.Dismiss();
                                LobbyMenu.Instance.Present();
                            });
                        }
                    }
                });
                Instance.SetRightViewController(rightViewController, false, (firstActivation, type) =>
                {
                    if (firstActivation)
                    {
                        refreshFriendsList();
                        rightViewController.CreateText("Invite Friends", new Vector2(BASE.x + 62.5f, BASE.y));

                        Button b = rightViewController.CreateUIButton("CreditsButton", new Vector2(BASE.x + 80f, BASE.y + 2.5f), new Vector2(25f, 7f));
                        b.SetButtonText("Refresh");
                        b.SetButtonTextSize(3f);
                        b.ToggleWordWrapping(false);
                        b.onClick.AddListener(delegate ()
                        {
                            refreshFriendsList();
                        });
                    }
                });

            }
        }
        
        
        private static void refreshFriendsList() 
        {
            friends = SteamAPI.GetOnlineFriends();
            rightViewController.Data.Clear();
            CGameID gameId = SteamAPI.GetGameID();
            foreach (KeyValuePair<CSteamID, string[]> entry in friends)
            {
                if ("" + gameId != entry.Value[1] || entry.Value[1] == "0")
                {
                    continue;
                }
                rightViewController.Data.Add(new CustomCellInfo(entry.Value[0], "Playing Beat Saber"));
            }
            foreach (KeyValuePair<CSteamID, string[]> entry in friends)
            {
                if ("" + gameId == entry.Value[1] || entry.Value[1] == "0")
                {
                    continue;
                }
                rightViewController.Data.Add(new CustomCellInfo(entry.Value[0], "Playing Other Game"));
            }
            foreach (KeyValuePair<CSteamID, string[]> entry in friends)
            {
                if ("0" != entry.Value[1])
                {
                    continue;
                }
                rightViewController.Data.Add(new CustomCellInfo(entry.Value[0], "Online"));
            }

            rightViewController._customListTableView.ReloadData();
            rightViewController._customListTableView.ScrollToRow(0, false);
            rightViewController.DidSelectRowEvent = (TableView view, int row) =>
            {
                var d = rightViewController.Data[row];
                foreach (KeyValuePair<CSteamID, string[]> pair in friends)
                {
                    if (d.text.Equals(pair.Value[0]))
                    {
                        SteamAPI.InviteUserToLobby(pair.Key);
                        break;
                    }
                }

            };
        }
        private static void refreshAvailableLobbies()
        {
            lobbies = new Dictionary<CSteamID, LobbyInfo>();

            if (refresh) refresh.interactable = true;
            if (!sorting)
            {
                SteamAPI.RequestLobbies();
            }
            else
            {
                SteamAPI.RequestAvailableLobbies();
            }
        }

        private static Dictionary<ulong, bool> availableLobbies = new Dictionary<ulong, bool>();
        public static void refreshLobbyList()
        {
            availableLobbies.Clear();
            middleViewController.Data.Clear();
            try
            {
                Logger.Info(SteamAPI.LobbyData.Count);
                Dictionary<ulong, LobbyInfo> lobbies = SteamAPI.LobbyData;
                foreach (KeyValuePair<ulong, LobbyInfo> entry in lobbies)
                {
                    LobbyInfo info = SteamAPI.LobbyData[entry.Key];
                    availableLobbies.Add(entry.Key, info.Joinable);
                    middleViewController.Data.Add(new CustomCellInfo($"{(info.Joinable ? "":"[LOCKED]")}[{info.UsedSlots}/{info.TotalSlots}] {info.HostName}'s Lobby", $"{info.Status}"));
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
                if (clickedID != 0 && availableLobbies.Values.ToArray()[row])
                {

                    Instance.Dismiss();
                    LobbyMenu.Instance.Present();
                    SteamAPI.JoinLobby(new CSteamID(clickedID));
                }
            };
            if (refresh) refresh.interactable = true;
        }
    }
}
