﻿using BeatSaberOnline.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace BeatSaberOnline.Data
{

    public class PlayerInfo
    {
        public string playerName;
        public ulong playerId;

        public uint playerScore;
        public uint playerCutBlocks;
        public uint playerComboBlocks;
        public uint playerTotalBlocks;
        public float playerEnergy;

        public float playerProgress;

        public Vector3 headPos;
        public Vector3 rightHandPos;
        public Vector3 leftHandPos;

        public Quaternion headRot;
        public Quaternion rightHandRot;
        public Quaternion leftHandRot;

        public string avatarHash;
        
        public PlayerInfo(string _name, ulong _id)
        {
            playerName = _name;
            playerId = _id;
            avatarHash = "FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF";
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
                playerComboBlocks = BitConverter.ToUInt32(data, 20 + nameLength);
                playerTotalBlocks = BitConverter.ToUInt32(data, 24 + nameLength);
                playerEnergy = BitConverter.ToSingle(data, 28 + nameLength);

                playerProgress = BitConverter.ToSingle(data, 32 + nameLength);

                byte[] avatar = data.Skip(36 + nameLength).Take(100).ToArray();

                rightHandPos = Serialization.ToVector3(avatar.Take(12).ToArray());
                leftHandPos = Serialization.ToVector3(avatar.Skip(12).Take(12).ToArray());
                headPos = Serialization.ToVector3(avatar.Skip(24).Take(12).ToArray());

                rightHandRot = Serialization.ToQuaternion(avatar.Skip(36).Take(16).ToArray());
                leftHandRot = Serialization.ToQuaternion(avatar.Skip(52).Take(16).ToArray());
                headRot = Serialization.ToQuaternion(avatar.Skip(68).Take(16).ToArray());

                avatarHash = BitConverter.ToString(avatar.Skip(84).Take(16).ToArray()).Replace("-", "");
        }

        private byte[] ToBytes(bool includeSize = true)
        {
            
            List<byte> buffer = new List<byte>();

            byte[] nameBuffer = Encoding.UTF8.GetBytes(playerName);
            buffer.AddRange(BitConverter.GetBytes(nameBuffer.Length));
            buffer.AddRange(nameBuffer);
            buffer.AddRange(BitConverter.GetBytes(playerId));

            buffer.AddRange(BitConverter.GetBytes(playerScore));
            buffer.AddRange(BitConverter.GetBytes(playerCutBlocks));
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

            if (includeSize)
                buffer.InsertRange(0, BitConverter.GetBytes(buffer.Count));

            return buffer.ToArray();
        }

        public override bool Equals(object obj)
        {
            if (obj is PlayerInfo)
            {
                return (playerId == (obj as PlayerInfo).playerId) && (playerName == (obj as PlayerInfo).playerName);
            }
            else
            {
                return false;
            }
        }

        public override int GetHashCode()
        {
            var hashCode = -2041759944;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(playerName);
            hashCode = hashCode * -1521134295 + playerId.GetHashCode();
            return hashCode;
        }

        public string Serialize()
        {
            return Convert.ToBase64String(ToBytes(false));
        }

        public byte[] DeSerialize(string body)
        {
            return Convert.FromBase64String(body);
        }
    }
}