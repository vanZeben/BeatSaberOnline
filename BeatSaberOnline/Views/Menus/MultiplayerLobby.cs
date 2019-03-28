using CustomUI.BeatSaber;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using SteamAPI = BeatSaberOnline.Data.Steam.SteamAPI;
using Steamworks;
using BeatSaberOnline.Data;
using Logger = BeatSaberOnline.Data.Logger;
using HMUI;
using System;
using BeatSaberOnline.Views.ViewControllers;
using System.Linq;
using VRUI;
using BeatSaberOnline.Workers;
using BeatSaberOnline.Data.Steam;

namespace BeatSaberOnline.Views.Menus
{
    class MultiplayerLobby : MonoBehaviour
    {
        static Vector2 BASE = new Vector2(-45f, 30f);
        public static CustomMenu Instance = null;
        private static Dictionary<CSteamID, string[]> friends;
        private static ulong selectedPlayer = 0;
        private static Button invite;
        private static Button rejoin;
        private static TableViewController rightViewController;
        private static TMPro.TextMeshProUGUI bodyText;
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
                                vc.SetButtonText(VoiceChatWorker.VoipEnabled ? "Disable Voice Chat" : "Enable Voice Chat");
                                vc.onClick.AddListener(delegate
                                {
                                    try
                                    {
                                        if (!VoiceChatWorker.VoipEnabled)
                                        {
                                            vc.SetButtonText("Disable Voice Chat");
                                            VoiceChatWorker.VoipEnabled = true;
                                            SteamUser.StartVoiceRecording();
                                        }
                                        else
                                        {
                                            vc.SetButtonText("Enable Voice Chat");
                                            VoiceChatWorker.VoipEnabled = false;
                                            SteamUser.StopVoiceRecording();
                                        }
                                    }
                                    catch (Exception e)
                                    {
                                        Logger.Error(e);
                                    }
                                });
                                offs += 10f;
                                rejoin = middleViewController.CreateUIButton("CreditsButton", new Vector2(BASE.x, BASE.y + 2.5f - offs), new Vector2(25f, 7f));
                                rejoin.SetButtonTextSize(3f);
                                rejoin.ToggleWordWrapping(false);
                                rejoin.SetButtonText("Re-Join Song");
                                rejoin.interactable = false;
                                rejoin.onClick.AddListener(delegate
                                {
                                    if (SteamAPI.GetLobbyData().Screen == LobbyPacket.SCREEN_TYPE.IN_GAME && SteamAPI.GetLobbyData().CurrentSongOffset > 0f)
                                    {
                                        WaitingMenu.autoReady = true;
                                        WaitingMenu.timeRequestedToLaunch = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds();
                                        WaitingMenu.Instance.Present();
                                    }
                                });
                                bodyText = middleViewController.CreateText("You can use Online Lobby in the Main Menu to choose songs for your lobby. \n\nYou can also control all the default Game Modifiers for the lobby through the Online Lobby Menu as well.", new Vector2(0, BASE.y - 10f));
                                var tt = middleViewController.CreateText("If something goes wrong, click the disconnect button above and just reconnect to the lobby.", new Vector2(0, 0 - BASE.y));
                                bodyText.alignment = TMPro.TextAlignmentOptions.Center;
                                tt.alignment = TMPro.TextAlignmentOptions.Center;
                            } catch(Exception e)
                            {
                                Logger.Error(e);
                            }

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
                    Instance.SetRightViewController(rightViewController, false, (firstActivation, type) => {
                        if (firstActivation)
                        {
                            rightViewController.CreateText("Lobby Leaderboard", new Vector2(BASE.x + 62.5f, BASE.y));
                        }
                        RefreshScores();
                    });
                } catch(Exception e)
                {
                    Logger.Error(e);
                }

            }
        }

        public static void UpdateJoinButton()
        {
            //if (!Instance || !Instance.isActiveAndEnabled) { return; }
            if (!SteamAPI.IsHost() && SteamAPI.GetLobbyData().Screen == LobbyPacket.SCREEN_TYPE.MENU) { bodyText.text = "Waiting for host to select a song"; }
            if (SteamAPI.GetLobbyData().Screen != LobbyPacket.SCREEN_TYPE.IN_GAME) { return; }
            bodyText.text = $"Currently playing {SteamAPI.GetSongName()} @ {Math.Floor(SteamAPI.GetSongOffset() / 60f)}:{Math.Floor(SteamAPI.GetSongOffset() % 60f)}";
            if (SteamAPI.GetLobbyData().CurrentSongOffset > 0f)
            {
                rejoin.interactable = true;
            }
            else
            {
                rejoin.interactable = false;
            }
        }

        private static Comparison<PlayerPacket> scoreComparison = new Comparison<PlayerPacket>((x, y) => (int) y.playerScore - (int) x.playerScore);
        public static void RefreshScores()
        {
            //if (!Instance || !Instance.isActiveAndEnabled) { return; }
            List<PlayerPacket> players = Controllers.PlayerController.Instance.GetConnectedPlayerPackets();
            players.Sort(scoreComparison);

            rightViewController.Data.Clear();
            rightViewController.Data = players;

            rightViewController._customListTableView.ReloadData();
            rightViewController._customListTableView.ScrollToCellWithIdx(0, TableView.ScrollPositionType.Beginning, false);

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
                Logger.Debug($"{entry.Value[0]} playing Beat Saber");
                leftViewController.Data.Add(new CustomCellInfo(entry.Value[0], "Playing Beat Saber"));
            }
            foreach (KeyValuePair<CSteamID, string[]> entry in friends)
            {
                if ("" + gameId == entry.Value[1] || entry.Value[1] == "0")
                {
                    continue;
                }
                Logger.Debug($"{entry.Value[0]} playing Other Game");
                leftViewController.Data.Add(new CustomCellInfo(entry.Value[0], "Playing Other Game"));
            }
            foreach (KeyValuePair<CSteamID, string[]> entry in friends)
            {
                if ("0" != entry.Value[1])
                {
                    continue;
                }
                Logger.Debug($"{entry.Value[0]} online");
                leftViewController.Data.Add(new CustomCellInfo(entry.Value[0], "Online"));
            }
            
            leftViewController._customListTableView.ReloadData();
            leftViewController._customListTableView.ScrollToCellWithIdx(0, TableView.ScrollPositionType.Beginning, false);
            leftViewController.DidSelectRowEvent = (view, row) =>
            {
                invite.interactable = false;
                CustomCellInfo cell = leftViewController.Data[row];
                KeyValuePair<CSteamID, string[]> friend = friends.Where(entry => entry.Value[0] == cell.text).First();
                selectedPlayer = friend.Key.m_SteamID;
                invite.interactable = true;
            };
        }
    }
}
