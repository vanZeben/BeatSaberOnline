using BeatSaberOnline.Data;
using BeatSaberOnline.Utils;
using CustomAvatar;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.XR;
using Logger = BeatSaberOnline.Data.Logger;
namespace BeatSaberOnline.Controllers
{
    public class AvatarController : MonoBehaviour, IAvatarInput
    {
        // Sourced from https://github.com/andruzzzhka/BeatSaberMultiplayer/blob/master/BeatSaberMultiplayer/AvatarController.cs
        static CustomAvatar.CustomAvatar defaultAvatarInstance;

        static List<CustomAvatar.CustomAvatar> pendingAvatars = new List<CustomAvatar.CustomAvatar>();
        static event Action<string, CustomAvatar.CustomAvatar> AvatarLoaded;

        PlayerInfo playerInfo;

        SpawnedAvatar avatar;

        TextMeshPro playerNameText;

        Vector3 targetHeadPos;
        Vector3 interpHeadPos;
        Vector3 lastHeadPos;

        Vector3 targetLeftHandPos;
        Vector3 interpLeftHandPos;
        Vector3 lastLeftHandPos;

        Vector3 targetRightHandPos;
        Vector3 interpRightHandPos;
        Vector3 lastRightHandPos;

        Quaternion targetHeadRot;
        Quaternion interpHeadRot;
        Quaternion lastHeadRot;

        Quaternion targetLeftHandRot;
        Quaternion interpLeftHandRot;
        Quaternion lastLeftHandRot;

        Quaternion targetRightHandRot;
        Quaternion interpRightHandRot;
        Quaternion lastRightHandRot;

        float interpolationProgress = 0f;

        bool rendererEnabled = true;
        Camera _camera;
        public bool forcePlayerInfo = false;

        VRCenterAdjust _centerAdjust;

        public PosRot HeadPosRot => new PosRot(interpHeadPos, interpHeadRot);

        public PosRot LeftPosRot => new PosRot(interpLeftHandPos, interpLeftHandRot);

        public PosRot RightPosRot => new PosRot(interpRightHandPos, interpRightHandRot);

        public static void LoadAvatars()
        {
            if (defaultAvatarInstance == null)
            {
                defaultAvatarInstance = CustomAvatar.Plugin.Instance.AvatarLoader.Avatars.FirstOrDefault(x => x.FullPath.ToLower().Contains("loading.avatar"));
            }
            Logger.Debug($"Found avatar, isLoaded={defaultAvatarInstance.IsLoaded}");
            if (!defaultAvatarInstance.IsLoaded)
            {
                defaultAvatarInstance.Load(null);
            }

            foreach (CustomAvatar.CustomAvatar avatar in CustomAvatar.Plugin.Instance.AvatarLoader.Avatars)
            {
                Task.Run(() =>
                {
                    string hash;
                    if (CreateMD5FromFile(avatar.FullPath, out hash))
                    {
                        ModelSaberAPI.cachedAvatars.Add(hash, avatar);
                        Logger.Debug("Hashed avatar " + avatar.Name + "! Hash: " + hash);
                    }
                }).ConfigureAwait(false);

            }
        }

        public AvatarController()
        {
            StartCoroutine(InitializeAvatarController());
        }

        IEnumerator InitializeAvatarController()
        {
            if (!defaultAvatarInstance.IsLoaded)
            {
                Logger.Debug("Waiting for avatar to load");
                yield return new WaitWhile(delegate () { return !defaultAvatarInstance.IsLoaded; });
            }
            else
            {
                yield return null;
            }

            Logger.Debug("Spawning avatar");
            _centerAdjust = FindObjectOfType<VRCenterAdjust>();

            avatar = AvatarSpawner.SpawnAvatar(defaultAvatarInstance, this);

            playerNameText = CreateWorldText(transform, "Loading");
            playerNameText.rectTransform.anchoredPosition3D = new Vector3(0f, 0.25f, 0f);
            playerNameText.alignment = TextAlignmentOptions.Center;
            playerNameText.fontSize = 2.5f;

            avatar.GameObject.transform.SetParent(_centerAdjust.transform, false);
            transform.SetParent(_centerAdjust.transform, false);
        }

        void Update()
        {
            try
            {
                if (avatar != null && !forcePlayerInfo)
                {
                    if (GameController.Tickrate < (1f / Time.smoothDeltaTime))
                    {
                        interpolationProgress += Time.deltaTime * GameController.Tickrate;
                    }
                    else
                    {
                        interpolationProgress = 1f;
                    }
                    if (interpolationProgress > 1f)
                    {
                        interpolationProgress = 1f;
                    }

                    interpHeadPos = Vector3.Lerp(lastHeadPos, targetHeadPos, interpolationProgress);
                    interpLeftHandPos = Vector3.Lerp(lastLeftHandPos, targetLeftHandPos, interpolationProgress);
                    interpRightHandPos = Vector3.Lerp(lastRightHandPos, targetRightHandPos, interpolationProgress);

                    interpHeadRot = Quaternion.Lerp(lastHeadRot, targetHeadRot, interpolationProgress);
                    interpLeftHandRot = Quaternion.Lerp(lastLeftHandRot, targetLeftHandRot, interpolationProgress);
                    interpRightHandRot = Quaternion.Lerp(lastRightHandRot, targetRightHandRot, interpolationProgress);

                    transform.position = interpHeadPos;
                }
            }
            catch (Exception e)
            {
                Logger.Error("Unable to lerp avatar position! Exception: " + e);
            }

            try
            {
                if (IllusionInjector.PluginManager.Plugins.Any(x => x.Name == "CameraPlus") && _camera == null)
                {
                    _camera = FindObjectsOfType<Camera>().FirstOrDefault(x => x.name == "Camera Plus");
                }
                if (playerNameText)
                {
                    playerNameText.rectTransform.rotation = Quaternion.LookRotation(playerNameText.rectTransform.position - WorldController.GetXRNodeWorldPosRot(XRNode.Head).Position);
                }

            }
            catch (Exception e)
            {
                Logger.Warning("Unable to rotate text to the camera! Exception: " + e);
            }

        }

        void OnDestroy()
        {
            Destroy(avatar.GameObject);
        }

        public void SetPlayerInfo(PlayerInfo _playerInfo, Vector3 offsetVector, bool isLocal)
        {
            if (_playerInfo == null)
            {
                playerNameText.gameObject.SetActive(false);
                if (rendererEnabled)
                {
                    SetRendererInChilds(avatar.GameObject.transform, false);
                    rendererEnabled = false;
                }
                return;
            }

            try
            {

                playerInfo = _playerInfo;
                
                if (!playerNameText)
                {
                    return;
                }
                    if (avatar == null || ModelSaberAPI.cachedAvatars.First(x => x.Value == avatar.CustomAvatar).Key != playerInfo.avatarHash)
                {
                    if (ModelSaberAPI.cachedAvatars.ContainsKey(playerInfo.avatarHash))
                    {
                        if (ModelSaberAPI.cachedAvatars[playerInfo.avatarHash].IsLoaded)
                        {
                            if (avatar != null)
                            {
                                Destroy(avatar.GameObject);
                            }

                            avatar = AvatarSpawner.SpawnAvatar(ModelSaberAPI.cachedAvatars[playerInfo.avatarHash], this);
                        }
                        else if (!pendingAvatars.Contains(ModelSaberAPI.cachedAvatars[playerInfo.avatarHash]))
                        {
                            pendingAvatars.Add(ModelSaberAPI.cachedAvatars[playerInfo.avatarHash]);
                            ModelSaberAPI.cachedAvatars[playerInfo.avatarHash].Load((CustomAvatar.CustomAvatar loadedAvatar, AvatarLoadResult result) =>
                            {
                                if (result == AvatarLoadResult.Completed)
                                {
                                    pendingAvatars.Remove(ModelSaberAPI.cachedAvatars[playerInfo.avatarHash]);
                                    AvatarLoaded?.Invoke(ModelSaberAPI.cachedAvatars.First(x => x.Value == avatar.CustomAvatar).Key, loadedAvatar);
                                }
                            });
                            AvatarLoaded += AvatarController_AvatarLoaded;
                        }
                        else
                        {
                            AvatarLoaded -= AvatarController_AvatarLoaded;
                            AvatarLoaded += AvatarController_AvatarLoaded;
                        }
                    }
                    else
                    {
                        if (ModelSaberAPI.queuedAvatars.Contains(playerInfo.avatarHash))
                        {
                            ModelSaberAPI.avatarDownloaded += AvatarDownloaded;
                        }
                        else
                        {
                            SharedCoroutineStarter.instance.StartCoroutine(ModelSaberAPI.DownloadAvatarCoroutine(playerInfo.avatarHash, (CustomAvatar.CustomAvatar avatar) => { AvatarDownloaded(playerInfo.avatarHash, avatar); }));
                        }
                    }
                }

                    if (isLocal)
                    {
                        playerNameText.gameObject.SetActive(false);
#if !DEBUG
                    if (rendererEnabled)
                    {
                        SetRendererInChilds(avatar.GameObject.transform, false);
                        rendererEnabled = false;
                    }
#endif
                    }
                    else
                    {
                        playerNameText.gameObject.SetActive(true);
                        if (!rendererEnabled)
                        {
                            SetRendererInChilds(avatar.GameObject.transform, true);
                            rendererEnabled = true;
                        }
                    }
                interpolationProgress = 0f;
                

                lastHeadPos = targetHeadPos;
                targetHeadPos = _playerInfo.headPos + offsetVector;

                lastRightHandPos = targetRightHandPos;
                targetRightHandPos = _playerInfo.rightHandPos + offsetVector;

                lastLeftHandPos = targetLeftHandPos;
                targetLeftHandPos = _playerInfo.leftHandPos + offsetVector;

                lastHeadRot = targetHeadRot;
                targetHeadRot = _playerInfo.headRot;

                lastRightHandRot = targetRightHandRot;
                targetRightHandRot = _playerInfo.rightHandRot;

                lastLeftHandRot = targetLeftHandRot;
                targetLeftHandRot = _playerInfo.leftHandRot;

                playerNameText.text = playerInfo.playerName;

                if (forcePlayerInfo)
                {
                    interpHeadPos = targetHeadPos;
                    interpLeftHandPos = targetLeftHandPos;
                    interpRightHandPos = targetRightHandPos;

                    interpHeadRot = targetHeadRot;
                    interpLeftHandRot = targetLeftHandRot;
                    interpRightHandRot = targetRightHandRot;

                    transform.position = interpHeadPos;
                }

            }
            catch (Exception e)
            {
                Logger.Error($"Avatar controller exception: {_playerInfo.playerName}: {e}");
            }

        }

        private void AvatarController_AvatarLoaded(string hash, CustomAvatar.CustomAvatar loadedAvatar)
        {
            if (ModelSaberAPI.cachedAvatars.First(x => x.Value == avatar.CustomAvatar).Key != playerInfo.avatarHash && playerInfo.avatarHash == hash)
            {
                AvatarLoaded -= AvatarController_AvatarLoaded;

                if (avatar != null)
                {
                    Destroy(avatar.GameObject);
                }

                avatar = AvatarSpawner.SpawnAvatar(loadedAvatar, this);
            }
        }

        private void AvatarDownloaded(string hash, CustomAvatar.CustomAvatar downloadedAvatar)
        {
            if (ModelSaberAPI.cachedAvatars.First(x => x.Value == avatar.CustomAvatar).Key != playerInfo.avatarHash && playerInfo.avatarHash == hash)
            {
                ModelSaberAPI.avatarDownloaded -= AvatarDownloaded;

                if (avatar != null)
                {
                    Destroy(avatar.GameObject);
                }

                avatar = AvatarSpawner.SpawnAvatar(downloadedAvatar, this);
            }
        }

        private void SetRendererInChilds(Transform origin, bool enabled)
        {
            Renderer[] rends = origin.gameObject.GetComponentsInChildren<Renderer>();
            foreach (Renderer rend in rends)
            {
                rend.enabled = enabled;
            }
        }


        public TextMeshPro CreateWorldText(Transform parent, string text = "TEXT")
        {
            TextMeshPro textMesh = new GameObject("CustomUIText").AddComponent<TextMeshPro>();
            textMesh.transform.SetParent(parent, false);
            textMesh.text = text;
            textMesh.fontSize = 5;
            textMesh.color = Color.white;
            textMesh.font = Resources.Load<TMP_FontAsset>("Teko-Medium SDF No Glow");
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