using BeatSaberOnline.Data;
using CustomUI.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Audio;

namespace BeatSaberOnline.Utils
{
    class Assets
    {
        private static AssetBundle _voipAssets = null;
        private static AudioMixerGroup _group = null;
        private static AssetBundle VoipAssets
        {
            get
            {
                if (!_voipAssets)
                    _voipAssets = AssetBundle.LoadFromMemory(UIUtilities.GetResource(Assembly.GetExecutingAssembly(), "BeatSaberOnline.Resources.VoipVolumeMixer"));
                return _voipAssets;
            }
        }
        public static AudioMixerGroup AudioGroup
        {
            get
            {
                if (!_group)
                {
                    try
                    {
                        AudioMixer mixer = VoipAssets.LoadAsset<AudioMixer>("AudioMixer");
                        _group = mixer.FindMatchingGroups("Master")[0];
                        mixer.SetFloat("MasterVolume", Config.Instance.Volume);
                    }
                    catch (Exception e)
                    {
                        Data.Logger.Error(e);
                    }
                }
                return _group;
            }
        }
    }
}
