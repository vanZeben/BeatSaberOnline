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
using UnityEngine.EventSystems;

namespace BeatSaberOnline.Views.Menus
{
    class MultiplayerLobby : MonoBehaviour
    {
        static Vector2 BASE = new Vector2(-45f, 30f);
        public static CustomMenu Instance = null;
        private static Dictionary<CSteamID, string[]> friends;
        private static ulong selectedPlayer = 0;
        private static Button invite;
        private static TableViewController rightViewController;
        public static void Init()
        {
            if (Instance == null)
            {
                try
                {
                    Instance = BeatSaberUI.CreateCustomMenu<CustomMenu>("Multiplayer Lobby");

                    CustomViewController middleViewController = BeatSaberUI.CreateViewController<CustomViewController>();
                    ListViewController leftViewController = BeatSaberUI.CreateViewController<ListViewController>();
                     rightViewController = BeatSaberUI.CreateViewController<TableViewController>();

                    Instance.SetMainViewController(middleViewController, true, (firstActivation, type) =>
                    {
                        if (firstActivation)
                        {
                            try
                            {
                                Button host = middleViewController.CreateUIButton("CreditsButton", new Vector2(BASE.x, BASE.y + 2.5f), new Vector2(25f, 7f));
                                host.SetButtonTextSize(3f);
                                host.ToggleWordWrapping(false);
                                host.SetButtonText("Disconnect");
                                host.onClick.AddListener(delegate
                                {
                                    try
                                    {
                                        SteamAPI.Disconnect();
                                        Instance.Dismiss();
                                        MultiplayerListing.Instance.Present();
                                    }
                                    catch (Exception e)
                                    {
                                        Logger.Error(e);
                                    }
                                });
                                float offs = 0;
                                offs += 10f;
                                Button vc = middleViewController.CreateUIButton("CreditsButton", new Vector2(BASE.x, BASE.y + 2.5f - offs), new Vector2(25f, 7f));
                                vc.SetButtonTextSize(3f);
                                vc.ToggleWordWrapping(false);
                                vc.SetButtonText(Controllers.PlayerController.Instance.VoipEnabled ? "Disable Voice Chat" : "Enable Voice Chat");
                                vc.onClick.AddListener(delegate
                                {
                                    try
                                    {
                                        if (!Controllers.PlayerController.Instance.VoipEnabled)
                                        {
                                            vc.SetButtonText("Disable Voice Chat");
                                            Controllers.PlayerController.Instance.VoipEnabled = true;
                                            SteamUser.StartVoiceRecording();
                                        }
                                        else
                                        {
                                            vc.SetButtonText("Enable Voice Chat");
                                            Controllers.PlayerController.Instance.VoipEnabled = false;
                                            SteamUser.StopVoiceRecording();
                                        }
                                    }
                                    catch (Exception e)
                                    {
                                        Logger.Error(e);
                                    }
                                });
                                var t = middleViewController.CreateText("You can use Online Lobby in the Main Menu to choose songs for your lobby. \n\nYou can also control all the default Game Modifiers for the lobby through the Online Lobby Menu as well.", new Vector2(0, BASE.y - 10f));
                                var tt = middleViewController.CreateText("If something goes wrong, click the disconnect button above and just reconnect to the lobby.", new Vector2(0, 0 - BASE.y));
                                t.alignment = TMPro.TextAlignmentOptions.Center;
                                tt.alignment = TMPro.TextAlignmentOptions.Center;
                            } catch(Exception e)
                            {
                                Data.Logger.Error(e);
                            }
                            /*   Button g = middleViewController.CreateUIButton("CreditsButton", new Vector2(0, 0), new Vector2(25f, 25f));
                               g.SetButtonTextSize(7f);
                               g.ToggleWordWrapping(false);
                               g.SetButtonText("Select a Song");
                               g.onClick.AddListener(delegate {
                                   try
                                   {
                                       if (SteamAPI.IsHost())
                                       {
                                           SteamAPI.SetSong("112D7FA45FA06F36FF41029099E95B98", "TaKillYa");
                                           SteamAPI.SetDifficulty((byte)2);
                                           SteamAPI.RequestPlay(new GameplayModifiers(new GameplayModifiers()));
                                       }
                                   }
                                   catch (Exception e)
                                   {
                                       Logger.Error(e);
                                   }
                               });*/
                        }
                    });
                    Instance.SetLeftViewController(leftViewController, false, (firstActivation, type) =>
                    {
                        if (firstActivation)
                        {
                            refreshFriendsList(leftViewController);
                            leftViewController.CreateText("Invite Friends", new Vector2(BASE.x + 62.5f, BASE.y));

                            Button b = leftViewController.CreateUIButton("CreditsButton", new Vector2(BASE.x + 80f, BASE.y + 2.5f), new Vector2(25f, 7f));
                            b.SetButtonText("Refresh");
                            b.SetButtonTextSize(3f);
                            b.ToggleWordWrapping(false);
                            b.onClick.AddListener(delegate ()
                            {
                                refreshFriendsList(leftViewController);
                            });


                            invite = leftViewController.CreateUIButton("CreditsButton", new Vector2(BASE.x + 80f, 0 - BASE.y + 2.5f), new Vector2(25f, 7f));
                            invite.SetButtonText("Invite");
                            invite.SetButtonTextSize(3f);
                            invite.ToggleWordWrapping(false);
                            invite.interactable = false;
                            invite.onClick.AddListener(delegate ()
                            {
                                if (selectedPlayer > 0)
                                {
                                    SteamAPI.InviteUserToLobby(new CSteamID(selectedPlayer));
                                }
                            });
                        }
                    });
                    Instance.SetRightViewController(rightViewController, false, (active, type) => {
                        if (active)
                        {
                            rightViewController.CreateText("Lobby Leaderboard", new Vector2(BASE.x + 62.5f, BASE.y));
                        }
                        RefreshScores();
                    });
                } catch(Exception e)
                {
                    Data.Logger.Error(e);
                }

            }
        }
        private static Comparison<PlayerInfo> scoreComparison = new Comparison<PlayerInfo>((x, y) => (int) y.playerScore - (int) x.playerScore);
        public static void RefreshScores()
        {
            if (!Instance.isActiveAndEnabled) { return; } 
            List<PlayerInfo> players = Controllers.PlayerController.Instance.GetConnectedPlayerInfos();
            players.Sort(scoreComparison);

            rightViewController.Data.Clear();
            rightViewController.Data = players;

            rightViewController._customListTableView.ReloadData();
            rightViewController._customListTableView.ScrollToRow(0, false);

        }

        private static FlowCoordinator GetActiveFlowCoordinator()
        {
            FlowCoordinator[] flowCoordinators = Resources.FindObjectsOfTypeAll<FlowCoordinator>();
            foreach (FlowCoordinator f in flowCoordinators)
            {
                if (f.isActivated)
                    return f;
            }
            return null;
        }

        private static void refreshFriendsList(ListViewController leftViewController) 
        {
            friends = SteamAPI.GetOnlineFriends();
            leftViewController.Data.Clear();
            CGameID gameId = SteamAPI.GetGameID();
            foreach (KeyValuePair<CSteamID, string[]> entry in friends)
            {
                if ("" + gameId != entry.Value[1] || entry.Value[1] == "0")
                {
                    continue;
                }
                leftViewController.Data.Add(new CustomCellInfo(entry.Value[0], "Playing Beat Saber"));
            }
            foreach (KeyValuePair<CSteamID, string[]> entry in friends)
            {
                if ("" + gameId == entry.Value[1] || entry.Value[1] == "0")
                {
                    continue;
                }
                leftViewController.Data.Add(new CustomCellInfo(entry.Value[0], "Playing Other Game"));
            }
            foreach (KeyValuePair<CSteamID, string[]> entry in friends)
            {
                if ("0" != entry.Value[1])
                {
                    continue;
                }
                leftViewController.Data.Add(new CustomCellInfo(entry.Value[0], "Online"));
            }

            leftViewController._customListTableView.ReloadData();
            leftViewController._customListTableView.ScrollToRow(0, false);
            leftViewController.DidSelectRowEvent = (TableView view, int row) =>
            {
                selectedPlayer = friends.Keys.ToArray()[row].m_SteamID;
                invite.interactable = true;
            };
        }
    }
}
