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

namespace BeatSaberOnline.Views.Menus
{
    class OnlineMenu
    {
        static Vector2 BASE = new Vector2(-40f, 30f);
        public static CustomMenu Instance = null;
        private static Dictionary<CSteamID, string[]> friends;
        private static Dictionary<CSteamID, string> lobbies;

        public static void Init()
        {
            if (Instance == null)
            {
                Instance = BeatSaberUI.CreateCustomMenu<CustomMenu>("Online Multiplayer");
                
                var middleViewController = BeatSaberUI.CreateViewController<ListViewController>();
                var rightViewController = BeatSaberUI.CreateViewController<ListViewController>();

              
                Instance.SetMainViewController(middleViewController, true, (firstActivation, type) =>
                {
                    if (firstActivation)
                    {
                        refreshAvailableLobbies(middleViewController);
                        middleViewController.CreateText("Available Lobbies", new Vector2(BASE.x + 60f, BASE.y));
                        Button refresh = middleViewController.CreateUIButton("CreditsButton", new Vector2(BASE.x + 80f, BASE.y + 2.5f), new Vector2(25f, 7f));
                        refresh.SetButtonText("Refresh");
                        refresh.SetButtonTextSize(3f);
                        refresh.ToggleWordWrapping(false);
                        refresh.onClick.AddListener(delegate ()
                        {
                            refreshAvailableLobbies(middleViewController);
                        });
                        Button host = middleViewController.CreateUIButton("CreditsButton", new Vector2(BASE.x, BASE.y + 2.5f), new Vector2(25f, 7f));
                        host.SetButtonTextSize(3f);
                        host.ToggleWordWrapping(false);
                        if (!Config.Instance.AutoStartLobby)
                        {
                            SetupHost(host);
                        }
                        else
                        {
                            SetupDisconnect(host);
                        }
                    }
                });
                Instance.SetRightViewController(rightViewController, false, (firstActivation, type) =>
                {
                    if (firstActivation)
                    {
                        refreshFriendsList(rightViewController);
                        rightViewController.CreateText("Invite Friends", new Vector2(BASE.x + 62.5f, BASE.y));

                        Button b = rightViewController.CreateUIButton("CreditsButton", new Vector2(BASE.x + 80f, BASE.y + 2.5f), new Vector2(25f, 7f));
                        b.SetButtonText("Refresh");
                        b.SetButtonTextSize(3f);
                        b.ToggleWordWrapping(false);
                        b.onClick.AddListener(delegate ()
                        {
                            refreshFriendsList(rightViewController);
                        });
                    }
                });

            }
        }

        private static void SetupDisconnect(Button host)
        {
            host.SetButtonText("Disconnect");
            host.onClick.AddListener(delegate ()
            {
                SteamAPI.Disconnect();
                SetupHost(host);
            });
        }
        private static void SetupHost(Button host)
        {
            host.SetButtonText("Host Lobby");
            host.onClick.AddListener(delegate ()
            {
                SteamAPI.GetServers();
                SetupDisconnect(host);
            });
        }
        private static void refreshFriendsList(ListViewController rightViewController) 
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
                        Logger.Info("Sending invite to " + pair.Value[0]);
                        SteamAPI.InviteUserToLobby(pair.Key);
                        break;
                    }
                }

            };
        }

        private static void refreshAvailableLobbies(ListViewController middleViewController)
        {
            lobbies = SteamAPI.getAvailableLobbies();
            middleViewController.Data.Clear();
            CGameID gameId = SteamAPI.GetGameID();
            foreach (KeyValuePair<CSteamID, string> entry in lobbies)
            {
                Dictionary<CSteamID, string> members = SteamAPI.GetMembersInLobby(entry.Key);
                string status = SteamAPI.getLobbyStatus(entry.Key);
                if (status == null) status = "Lobby";
                string membersStr = "";
                string[] memberStrs = members.Values.ToArray();
                for (int i = 0; i < memberStrs.Length;i++)
                {
                    membersStr += memberStrs[i] + (i >= memberStrs.Length - 1? "" : ", ");
                }
                middleViewController.Data.Add(new CustomCellInfo($"{status}", $"{membersStr}"));
            }

            middleViewController._customListTableView.ReloadData();
            middleViewController._customListTableView.ScrollToRow(0, false);
            middleViewController.DidSelectRowEvent = (TableView view, int row) =>
            {
                var d = middleViewController.Data[row];
                foreach (KeyValuePair<CSteamID, string> pair in lobbies)
                {
                    if (d.subtext.Equals($"~ {pair.Value}"))
                    {
                        Logger.Info("Joining " + pair.Value);
                        SteamAPI.JoinLobby(pair.Key);
                        break;
                    }
                }

            };
        }
    }
}
