using Steamworks;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using ConnectionState = BeatSaberOnline.Data.Steam.SteamAPI.ConnectionState;

namespace BeatSaberOnline.Data.Steam
{
    public class LobbyInfo
    {
        public enum SCREEN_TYPE : byte
        {
            NONE,
            WAITING,
            MENU,
            LOBBY,
            DOWNLOADING,
            PLAY_SONG,
            IN_GAME,
        }

        public string HostName { get; set; } = "";
        public CSteamID LobbyID { get; set; } = new CSteamID(0);

        public string Status { get; set; } = "";
        public bool Joinable { get; set; } = true;

        public int UsedSlots { get; set; } = 1;
        public int TotalSlots { get; set; } = 5;
        public int MaxSlots { get; private set; } = 10;

        public string CurrentSongId { get; set; } = "";
        public string CurrentSongName { get; set; } = "";
        public byte CurrentSongDifficulty { get; set; } = 0;
        public float CurrentSongOffset { get; set; } = 0f;

        public SCREEN_TYPE Screen { get; set; } = SCREEN_TYPE.NONE;
        private string _gameplayModifiers = "";

        public GameplayModifiers GameplayModifiers
        {
            get => new GameplayModifiers(JsonUtility.FromJson<GameplayModifiers>(_gameplayModifiers));
            set => _gameplayModifiers = JsonUtility.ToJson(value);
        }
        public LobbyInfo() {
            GameplayModifiers = new GameplayModifiers();
        }
        public LobbyInfo(string data)
        {
            FromBytes(DeSerialize(data));
        }
        private void FromBytes(byte[] data)
        {
            int currentStringPadding = BitConverter.ToInt32(data, 0);
            HostName = Encoding.UTF8.GetString(data, 4, currentStringPadding);
            LobbyID = new CSteamID(BitConverter.ToUInt64(data, 4 + currentStringPadding));

            int statusLength = BitConverter.ToInt32(data, 12 + currentStringPadding);
            Status = Encoding.UTF8.GetString(data, 16 + currentStringPadding, statusLength);
            currentStringPadding += statusLength;

            Joinable = BitConverter.ToBoolean(data, 16 + currentStringPadding);
            UsedSlots = BitConverter.ToInt32(data, 17 + currentStringPadding);
            TotalSlots = BitConverter.ToInt32(data, 21 + currentStringPadding);
            MaxSlots = BitConverter.ToInt32(data, 25 + currentStringPadding);

            statusLength = BitConverter.ToInt32(data, 29 + currentStringPadding);
            CurrentSongId = Encoding.UTF8.GetString(data, 33 + currentStringPadding, statusLength);
            currentStringPadding += statusLength;

            statusLength = BitConverter.ToInt32(data, 33 + currentStringPadding);
            CurrentSongName = Encoding.UTF8.GetString(data, 37 + currentStringPadding, statusLength);
            currentStringPadding += statusLength;

            CurrentSongDifficulty = data[37 + currentStringPadding];
            CurrentSongOffset = BitConverter.ToSingle(data, 38 + currentStringPadding);

            Screen = (SCREEN_TYPE) data[42 + currentStringPadding];

            statusLength = BitConverter.ToInt32(data, 43 + currentStringPadding);
            _gameplayModifiers = Encoding.UTF8.GetString(data, 47 + currentStringPadding, statusLength);
            currentStringPadding += statusLength;
        }

        private byte[] ToBytes(bool includeSize = true)
        {
            List<byte> buffer = new List<byte>();
            byte[] nameBuffer = Encoding.UTF8.GetBytes(HostName);
            buffer.AddRange(BitConverter.GetBytes(nameBuffer.Length));
            buffer.AddRange(nameBuffer);

            buffer.AddRange(BitConverter.GetBytes(LobbyID.m_SteamID));

            nameBuffer = Encoding.UTF8.GetBytes(Status);
            buffer.AddRange(BitConverter.GetBytes(nameBuffer.Length));
            buffer.AddRange(nameBuffer);

            buffer.AddRange(BitConverter.GetBytes(Joinable));
            buffer.AddRange(BitConverter.GetBytes(UsedSlots));
            buffer.AddRange(BitConverter.GetBytes(TotalSlots));
            buffer.AddRange(BitConverter.GetBytes(MaxSlots));

            nameBuffer = Encoding.UTF8.GetBytes(CurrentSongId);
            buffer.AddRange(BitConverter.GetBytes(nameBuffer.Length));
            buffer.AddRange(nameBuffer);

            nameBuffer = Encoding.UTF8.GetBytes(CurrentSongName);
            buffer.AddRange(BitConverter.GetBytes(nameBuffer.Length));
            buffer.AddRange(nameBuffer);
            buffer.Add(CurrentSongDifficulty);
            buffer.AddRange(BitConverter.GetBytes(CurrentSongOffset));

            buffer.Add((byte) Screen);
            
            nameBuffer = Encoding.UTF8.GetBytes(_gameplayModifiers);
            buffer.AddRange(BitConverter.GetBytes(nameBuffer.Length));
            buffer.AddRange(nameBuffer);

            if (includeSize)
                buffer.InsertRange(0, BitConverter.GetBytes(buffer.Count));

            return buffer.ToArray();
        }

        public override bool Equals(object obj)
        {
            if (obj is PlayerInfo)
            {
                return (LobbyID == (obj as LobbyInfo).LobbyID);
            }
            else
            {
                return false;
            }
        }
        public override int GetHashCode()
        {
            return unchecked(this.LobbyID.m_SteamID.GetHashCode() * 17 + this.HostName.GetHashCode());
        }
        public override string ToString()
        {
            return $"lobbyId={LobbyID},hostname={HostName},status={Status},joinable={Joinable},UsedSlots={UsedSlots},TotalSlots={TotalSlots},MaxSlots={MaxSlots},CurrentSongId={CurrentSongId},CurrentSongDifficulty={CurrentSongDifficulty},CurretnSongName={CurrentSongName},CurrentSongOffset={CurrentSongOffset},Screen={Screen},gameplayModifiers={_gameplayModifiers}";
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
