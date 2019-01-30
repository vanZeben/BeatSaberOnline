using UnityEngine;
using Steamworks;
using System;
using Logger = BeatSaberOnline.Data.Logger;
namespace BeatSaberOnline.Workers
{
    public class VoiceChatWorker : MonoBehaviour
    {
        public static VoiceChatWorker Instance;

        public static void Init()
        {
            if (Instance != null)
            {
                return;
            }
            new GameObject("VoiceChatWorker").AddComponent<VoiceChatWorker>();
        }

        void Awake()
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        void Update()
        {

            uint size;
            try
            {
                while (SteamUser.GetAvailableVoice(out size) == EVoiceResult.k_EVoiceResultOK && size > 1024)
                {
                    byte[] buffer = new byte[size];
                    uint bytesWritten;
                    if (SteamUser.GetVoice(true, buffer, size, out bytesWritten) == EVoiceResult.k_EVoiceResultOK && bytesWritten > 0)
                    {
                        Controllers.PlayerController.Instance._playerInfo.voip = buffer;
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }
    }
}