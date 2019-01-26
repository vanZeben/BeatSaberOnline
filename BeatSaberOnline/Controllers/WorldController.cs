using CustomAvatar;
using UnityEngine;
using UnityEngine.XR;

namespace BeatSaberOnline.Controllers
{
    class WorldController
    {

        public static Quaternion oculusTouchRotOffset = Quaternion.Euler(-40f, 0f, 0f);
        public static Vector3 oculusTouchPosOffset = new Vector3(0f, 0f, 0.055f);
        public static Quaternion openVrRotOffset = Quaternion.Euler(-4.3f, 0f, 0f);
        public static Vector3 openVrPosOffset = new Vector3(0f, -0.008f, 0f);

        public static PosRot GetXRNodeWorldPosRot(XRNode node)
        {
            var pos = InputTracking.GetLocalPosition(node);
            var rot = InputTracking.GetLocalRotation(node);

            var roomCenter = BeatSaberUtil.GetRoomCenter();
            var roomRotation = BeatSaberUtil.GetRoomRotation();

            pos = roomRotation * pos;
            pos += roomCenter;
            rot = roomRotation * rot;
            return new PosRot(pos, rot);
        }
        private class HandOffset
        {
            public Quaternion LeftHandRot { get; set; }
            public Vector3 LeftHandPos { get; set; }
        }
        private static HandOffset GetLeftHandOffs()
        {

            if (PersistentSingleton<VRPlatformHelper>.instance.vrPlatformSDK == VRPlatformHelper.VRPlatformSDK.Oculus)
            {
                return new HandOffset
                {
                    LeftHandRot = WorldController.oculusTouchRotOffset,
                    LeftHandPos = WorldController.oculusTouchPosOffset
                };
            }
            else if (PersistentSingleton<VRPlatformHelper>.instance.vrPlatformSDK == VRPlatformHelper.VRPlatformSDK.OpenVR)
            {
                return new HandOffset
                {
                    LeftHandRot = WorldController.openVrRotOffset,
                    LeftHandPos = WorldController.openVrPosOffset
                };
            }
            return null;
        }

        public class CharacterPosition
        {
            public Vector3 headPos { get; set; }
            public Vector3 leftHandPos { get; set; }
            public Vector3 rightHandPos { get; set; }
            public Quaternion headRot { get; set; }
            public Quaternion leftHandRot { get; set; }
            public Quaternion rightHandRot { get; set; }
        }

        public static CharacterPosition GetCharacterInfo()
        {
            HandOffset leftOffs = GetLeftHandOffs();
            return new CharacterPosition
            {
                headPos = WorldController.GetXRNodeWorldPosRot(XRNode.Head).Position,
                headRot = WorldController.GetXRNodeWorldPosRot(XRNode.Head).Rotation,
                leftHandPos = WorldController.GetXRNodeWorldPosRot(XRNode.LeftHand).Position + leftOffs.LeftHandPos,
                leftHandRot = WorldController.GetXRNodeWorldPosRot(XRNode.LeftHand).Rotation * leftOffs.LeftHandRot,

                rightHandPos = WorldController.GetXRNodeWorldPosRot(XRNode.RightHand).Position,
                rightHandRot = WorldController.GetXRNodeWorldPosRot(XRNode.RightHand).Rotation
            };
        }
    }
}
