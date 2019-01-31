using BeatSaberOnline.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace BeatSaberOnline.Data
{

    public class PlayerInfo
    {
        // Based on https://github.com/andruzzzhka/BeatSaberMultiplayer/blob/master/BeatSaberMultiplayer/Data/PlayerInfo.cs
        public string playerName = "";
        public ulong playerId = 0;

        public uint playerScore = 0;
        public uint playerCutBlocks = 0;
        public uint playerComboBlocks = 0;
        public uint playerMaxComboBlocks = 0;
        public uint playerTotalBlocks = 0;
        public float playerEnergy = 0;

        public float playerProgress = 0f;

        public Vector3 headPos = new Vector3(0, 0, 0);
        public Vector3 rightHandPos = new Vector3(0, 0, 0);
        public Vector3 leftHandPos = new Vector3(0, 0, 0);

        public Quaternion headRot = new Quaternion();
        public Quaternion rightHandRot = new Quaternion();
        public Quaternion leftHandRot = new Quaternion();
        
        public string avatarHash = "FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF";
        public bool Downloading = false;
        public byte[] voip = new byte[0];

        public PlayerInfo(string _name, ulong _id)
        {
            playerName = _name;
            playerId = _id;
        }
        public PlayerInfo(string data)
        {
            FromBytes(DeSerialize(data));
        }
        private void FromBytes(byte[] data)
        {
                int nameLength = BitConverter.ToInt32(data, 0);
                playerName = Encoding.UTF8.GetString(data, 4, nameLength);
                playerId = BitConverter.ToUInt64(data, 4 + nameLength);

                playerScore = BitConverter.ToUInt32(data, 12 + nameLength);
                playerCutBlocks = BitConverter.ToUInt32(data, 16 + nameLength);
                playerMaxComboBlocks = BitConverter.ToUInt32(data, 20 + nameLength);
                playerComboBlocks = BitConverter.ToUInt32(data, 24 + nameLength);
                playerTotalBlocks = BitConverter.ToUInt32(data, 28+ nameLength);
                playerEnergy = BitConverter.ToSingle(data, 32 + nameLength);

                playerProgress = BitConverter.ToSingle(data, 36 + nameLength);

                byte[] avatar = data.Skip(40 + nameLength).Take(100).ToArray();

                rightHandPos = Serialization.ToVector3(avatar.Take(12).ToArray());
                leftHandPos = Serialization.ToVector3(avatar.Skip(12).Take(12).ToArray());
                headPos = Serialization.ToVector3(avatar.Skip(24).Take(12).ToArray());

                rightHandRot = Serialization.ToQuaternion(avatar.Skip(36).Take(16).ToArray());
                leftHandRot = Serialization.ToQuaternion(avatar.Skip(52).Take(16).ToArray());
                headRot = Serialization.ToQuaternion(avatar.Skip(68).Take(16).ToArray());

                avatarHash = BitConverter.ToString(avatar.Skip(84).Take(16).ToArray()).Replace("-", "");

                Downloading = BitConverter.ToBoolean(data, 140 + nameLength);

                int voipLength = BitConverter.ToInt32(data, 141 + nameLength);
                voip = data.Skip(145 + nameLength).Take(voipLength).ToArray();
        }

        private byte[] GetBytes()
        {
            List<byte> buffer = new List<byte>();

            byte[] nameBuffer = Encoding.UTF8.GetBytes(playerName);
            buffer.AddRange(BitConverter.GetBytes(nameBuffer.Length));
            buffer.AddRange(nameBuffer);
            buffer.AddRange(BitConverter.GetBytes(playerId));

            buffer.AddRange(BitConverter.GetBytes(playerScore));
            buffer.AddRange(BitConverter.GetBytes(playerCutBlocks));
            buffer.AddRange(BitConverter.GetBytes(playerMaxComboBlocks));
            buffer.AddRange(BitConverter.GetBytes(playerComboBlocks));
            buffer.AddRange(BitConverter.GetBytes(playerTotalBlocks));
            buffer.AddRange(BitConverter.GetBytes(playerEnergy));

            buffer.AddRange(BitConverter.GetBytes(playerProgress));

            buffer.AddRange(Serialization.Combine(
                            Serialization.ToBytes(rightHandPos),
                            Serialization.ToBytes(leftHandPos),
                            Serialization.ToBytes(headPos),
                            Serialization.ToBytes(rightHandRot),
                            Serialization.ToBytes(leftHandRot),
                            Serialization.ToBytes(headRot)));

            buffer.AddRange(HexConverter.ConvertHexToBytesX(avatarHash));

            buffer.AddRange(BitConverter.GetBytes(Downloading));
            buffer.AddRange(BitConverter.GetBytes(voip.Length));
            buffer.AddRange(voip);
            
            return buffer.ToArray();
        }

        public override bool Equals(object obj)
        {
            return obj is PlayerInfo && (playerId == (obj as PlayerInfo).playerId);
        }

        public override int GetHashCode()
        {
            return unchecked(this.playerId.GetHashCode() * 17 + this.playerName.GetHashCode());
        }

        public string Serialize()
        {
            return Convert.ToBase64String(GetBytes());
        }

        public byte[] DeSerialize(string body)
        {
            return Convert.FromBase64String(body);
        }
    }
}
