using BeatSaberOnline.Utils;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BeatSaberOnline.Data
{

    public class VoipPacket
    {


        public byte[] voip = new byte[0];

        public VoipPacket(byte[] data)
        {
            voip = data;
        }
        public VoipPacket(string data)
        {
            FromBytes(DeSerialize(data));
        }
        private void FromBytes(byte[] data)
        {
            voip = data;
        }
        
        public bool Play(AudioSource source)
        {
            byte[] voipBuffer = new byte[11025 * 2];
            uint byteLength;

            if (SteamUser.DecompressVoice(voip, (uint)voip.Length, voipBuffer, (uint)voipBuffer.Length, out byteLength, 11025) == EVoiceResult.k_EVoiceResultOK && byteLength > 0)
            {
                float[] v = new float[11025];
                for (int i = 0; i < v.Length; ++i)
                {
                    v[i] = (short)(voipBuffer[i * 2] | voipBuffer[i * 2 + 1] << 8) / 32768.0f;
                }
                source.clip.SetData(v, 0);
                source.outputAudioMixerGroup = Utils.Assets.AudioGroup;
                source.Play();
                return true;
            }
            return false;
        }


        public override int GetHashCode()
        {
            return unchecked(this.voip.GetHashCode() * 17);
        }

        public string Serialize()
        {
            return Convert.ToBase64String(voip);
        }

        public byte[] DeSerialize(string body)
        {
            return Convert.FromBase64String(body);
        }
    }
}
