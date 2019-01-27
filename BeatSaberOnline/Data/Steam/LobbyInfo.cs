using Steamworks;
using System;
using System.Collections.Generic;
using System.Text;
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
        public string HostName { get; set; }
        public CSteamID LobbyID { get; set; }

        public string Status { get; set; }
        public bool Joinable { get; set; } = true;

        public int UsedSlots { get; set; } = 1;
        public int TotalSlots { get; set; } = 5;
        public int MaxSlots { get; private set; } = 10;
        
        public string CurrentSongId { get; set; }
        public string CurrentSongName { get; set; }
        public byte CurrentSongDifficulty { get; set; }

        public SCREEN_TYPE Screen { get; set; }

        public LobbyInfo() { }
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

            Screen = (SCREEN_TYPE) data[38 + currentStringPadding];
        }

        private byte[] ToBytes(bool includeSize = true)
        {
            Logger.Info($"\nHostName={HostName}" +
                $"\nLobbyId: {LobbyID.m_SteamID}" +
                $"\nStatus: {Status}" +
                $"\nJoinable: {Joinable}" +
                $"\nUsedSlots: {UsedSlots}" +
                $"\nTotalSlots: {TotalSlots} " +
                $"\nMaxSlots: {MaxSlots}" +
                $"\nCurrentSongId: {CurrentSongId}" +
                $"\nCurrentSongDifficulty: {CurrentSongDifficulty}" +
                $"\nCurrentSongName: {CurrentSongName}" +
                $"\nScreen: {Screen}");
            Logger.Info(0);
            List<byte> buffer = new List<byte>();
            Logger.Info(1);
            byte[] nameBuffer = Encoding.UTF8.GetBytes(HostName);
            buffer.AddRange(BitConverter.GetBytes(nameBuffer.Length));
            buffer.AddRange(nameBuffer);
            Logger.Info(2);

            buffer.AddRange(BitConverter.GetBytes(LobbyID.m_SteamID));

            Logger.Info(4);
            nameBuffer = Encoding.UTF8.GetBytes(Status);
            buffer.AddRange(BitConverter.GetBytes(nameBuffer.Length));
            buffer.AddRange(nameBuffer);

            Logger.Info(4);
            buffer.AddRange(BitConverter.GetBytes(Joinable));
            Logger.Info(5);
            buffer.AddRange(BitConverter.GetBytes(UsedSlots));
            Logger.Info(6);
            buffer.AddRange(BitConverter.GetBytes(TotalSlots));
            Logger.Info(7);
            buffer.AddRange(BitConverter.GetBytes(MaxSlots));
            Logger.Info(8);

            nameBuffer = Encoding.UTF8.GetBytes(CurrentSongId);
            buffer.AddRange(BitConverter.GetBytes(nameBuffer.Length));
            buffer.AddRange(nameBuffer);
            Logger.Info(9);

            nameBuffer = Encoding.UTF8.GetBytes(CurrentSongName);
            buffer.AddRange(BitConverter.GetBytes(nameBuffer.Length));
            buffer.AddRange(nameBuffer);
            Logger.Info(10);
            buffer.Add(CurrentSongDifficulty);
            Logger.Info(11);
            buffer.Add((byte) Screen);
            Logger.Info(12);



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
