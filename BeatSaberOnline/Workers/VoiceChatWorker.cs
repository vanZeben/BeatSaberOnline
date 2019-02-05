using UnityEngine;
using Steamworks;
using System;
using Logger = BeatSaberOnline.Data.Logger;
using BeatSaberOnline.Data;
using System.Threading;
using System.Collections;
using System.Text;

namespace BeatSaberOnline.Workers
{
    public class VoiceChatWorker : MonoBehaviour
    {
        private static VoiceListener Listener;
        private static VoiceReceiver Receiver;
        private static bool _voipEnabled = false;
        public static bool VoipEnabled
        {
            get {
                return _voipEnabled;
            }
            set
            {
                _voipEnabled = value;
                if (!_voipEnabled)
                {
                    Listener.Stop();
                    Receiver.Stop();
                } else if (_voipEnabled)
                {
                    Listener = new VoiceListener();
                    Receiver = new VoiceReceiver();
                }
            }
        }

        public static void Init()
        {
            new GameObject("VoiceChatWorker").AddComponent<VoiceChatWorker>();
        }

        void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }
        
        public class VoiceListener
        {
            private Thread _thread { get; }
            private bool _stopped = false;
            public void Stop()
            {
                _stopped = true;
            }
            public VoiceListener()
            {
                
                _thread = new Thread(() =>
                {
                    while (!_stopped)
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
                                    Data.Steam.SteamAPI.SendVoip(new VoipPacket(buffer));
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            Logger.Error(e);
                        }
                    }
                });
                _thread.Start();
            }
        }


        public class VoiceReceiver
        {
            private Thread _thread { get; }
            private AudioSource source;
            private bool _stopped = false;
            private GameObject _voiceLoopback;
            public void Stop()
            {
                _stopped = true;
            }
            public VoiceReceiver()
            {
                if (_voiceLoopback == null)
                {
                    _voiceLoopback = new GameObject("Voice Loopback");
                    DontDestroyOnLoad(_voiceLoopback.gameObject);
                    source = _voiceLoopback.AddComponent<AudioSource>();
                    source.clip = AudioClip.Create("Voice Clip", 11025, 1, 11025, false);
                    source.volume = 1.0f;
                } else
                {
                    source = _voiceLoopback.GetComponent<AudioSource>();
                }
                _thread = new Thread(() =>
                {
                    while (!_stopped)
                    {
                        uint size;
                        try
                        {
                            while (SteamNetworking.IsP2PPacketAvailable(out size, 1))
                            {
                                var buffer = new byte[size];
                                uint bytesRead;
                                CSteamID remoteId;
                                if (SteamNetworking.ReadP2PPacket(buffer, size, out bytesRead, out remoteId, 1))
                                {
                                    var message = Encoding.UTF8.GetString(buffer).Replace(" ", "");
                                    VoipPacket info = new VoipPacket(message);

                                    info.Play(source);
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            Data.Logger.Error(e);
                        }
                    }
                });
                _thread.Start();
            }
        }

        public class ThreadedAction
        {
            private bool _isDone = false;
            public ThreadedAction(Action action)
            {
                var thread = new Thread(() =>
                {
                    if (action != null)
                        action();
                    _isDone = true;
                });
                thread.Start();
            }

            public IEnumerator WaitForComplete()
            {
                while (!_isDone)
                    yield return null;
            }
        }
    }
}