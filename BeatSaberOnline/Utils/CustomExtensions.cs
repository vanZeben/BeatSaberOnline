using BeatSaberOnline.Data;
using CustomUI.Utilities;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BeatSaberOnline.Utils
{

    public static class CustomExtensions
    {

        private static AssetBundle _textShaderAssets = null;
        private static AssetBundle TextShaderAsset
        {
            get
            {
                if (!_textShaderAssets)
                    _textShaderAssets = AssetBundle.LoadFromMemory(UIUtilities.GetResource(Assembly.GetExecutingAssembly(), "BeatSaberOnline.Resources.TextShader"));
                return _textShaderAssets;
            }
        }
        private static Material _testMaterial = null;
        public static Material Material
        {
            get
            {
                if (!_testMaterial)
                    _testMaterial = new Material(TextShaderAsset.LoadAsset<Shader>("TestFontShader"));
                return _testMaterial;
            }
        }


        public static void SetButtonStrokeColor(this Button btn, Color color)
        {
            btn.GetComponentsInChildren<Image>().First(x => x.name == "Stroke").color = color;
        }

        public static int FindIndexInList(this List<PlayerInfo> list, PlayerInfo _player)
        {
            return list.FindIndex(x => (x.playerId == _player.playerId) && (x.playerName == _player.playerName));
        }

        public static TextMeshPro CreateWorldText(Transform parent, string text = "TEXT")
        {
            TextMeshPro textMesh = new GameObject("CustomUIText").AddComponent<TextMeshPro>();
            textMesh.transform.SetParent(parent, false);
            textMesh.text = text;
            textMesh.fontSize = 5;
            textMesh.color = Color.white;
            textMesh.font = Resources.Load<TMP_FontAsset>("Teko-Medium SDF No Glow");
            textMesh.material = Material;
            return textMesh;
        }

        public static T CreateInstance<T>(params object[] args)
        {
            var type = typeof(T);
            var instance = type.Assembly.CreateInstance(
                type.FullName, false,
                BindingFlags.Instance | BindingFlags.NonPublic,
                null, args, null, null);
            return (T)instance;
        }

        public static bool CreateMD5FromFile(string path, out string hash)
        {
            hash = "";
            if (!File.Exists(path)) return false;
            using (MD5 md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(path))
                {
                    byte[] hashBytes = md5.ComputeHash(stream);

                    StringBuilder sb = new StringBuilder();
                    foreach (byte hashByte in hashBytes)
                    {
                        sb.Append(hashByte.ToString("X2"));
                    }

                    hash = sb.ToString();
                    return true;
                }
            }
        }

    }
}
