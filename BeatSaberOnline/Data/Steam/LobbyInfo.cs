using Steamworks;
using System.Collections.Generic;
using ConnectionState = BeatSaberOnline.Data.Steam.SteamAPI.ConnectionState;

namespace BeatSaberOnline.Data.Steam
{
    class LobbyInfo
    {
        public CSteamID LobbyID { get; set; }
 
        public bool Joinable { get; set; } = true;
        public int TotalSlots { get; set; } = 5;
        public int MaxSlots { get; private set; } = 10;
        public ConnectionState Connection { get; set; } = ConnectionState.UNDEFINED;
        public string SongId { get; set; }
        public byte SongDifficulty { get; set; }
        public Dictionary<CSteamID, bool> ReadyState = new Dictionary<CSteamID, bool>();

        public string toString()
        {
            return $"lobbyID={LobbyID.m_SteamID},joinable={Joinable},TotalSlots={TotalSlots},MaxSlots={MaxSlots}";
        }
    }
}
