using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSGO_Data_Uploader
{
    public class PlayerData
    {
        public long SteamID;
        public string Name;
        public string MatchID;
        public string Map;
        public string WinLossDraw;
        public int TotalRoundsPlayed;
        public int RoundsWon;
        public int RoundsLost;
        public int Kills;
        public int Deaths;
        public int Assists;
        public int MVPs;
        public int Score;
        public int BombPlants;
        public int BombDefuses;
        public int Headshots;
        public int MoneySpent;
        public int TeamKills;
        public int NoKills;
        public int OneKs;
        public int Twoks;
        public int Threeks;
        public int Fourks;
        public int Aces;
        public int EntryFrags;
        public int FinalKills;


        public PlayerData(long inSteamID, string inName)
        {
            SteamID = inSteamID;
            Name = inName;
        }

    }
}
