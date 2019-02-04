using BeatSaberOnline.Data.Steam;
using BeatSaberOnline.Views.ViewControllers;
using Harmony;
using System;

namespace BeatSaberOnline.Harmony
{
    [HarmonyPatch(typeof(PartyFreePlayFlowCoordinator),
        new Type[] {
            typeof(StandardLevelDetailViewController)})]
    [HarmonyPatch("HandleLevelDetailViewControllerDidPressPlayButton", MethodType.Normal)]
    class HandleLevelDetailViewControllerDidPressPlayButtonHook
    {

        static bool Prefix(StandardLevelDetailViewController viewController)
        {
            if (SteamAPI.isLobbyConnected())
            {
                MockPartyViewController.Instance.didSelectPlay();
                return false;
            }
            return true;
        }
    }
}
