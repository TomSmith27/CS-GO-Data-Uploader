using DemoInfo;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace CSGO_Data_Uploader
{
    public class Parser
    {
        public Dictionary<long, PlayerData> OurSteamIDs = new Dictionary<long, PlayerData>() 
        {
           {  76561197974813437, new PlayerData(76561197974813437,"Tom") },
           {  76561198037768649, new PlayerData(76561198037768649,"Jack") },
           {  76561198001391257, new PlayerData(76561198001391257,"Jake") },
           {  76561198002911310, new PlayerData(76561198002911310,"Matt") },
           {  76561197978834746, new PlayerData(76561197978834746,"Ting") },
           {  76561198099837947, new PlayerData(76561198099837947,"Rich") }
        };
        public bool OurSteamID(long steamID)
        {
            if (OurSteamIDs.ContainsKey(steamID))
            {
                return true;
            }
            return false;
        }
        public void ParseGame(string fileName)
        {
          
            {
                // Okay, first we need to initalize a demo-parser
                // It takes a stream, so we simply open with a filestream
                using (var fileStream = File.OpenRead(fileName))
                {
                    // By using "using" we make sure that the fileStream is properly disposed
                    // the same goes for the DemoParser which NEEDS to be disposed (else it'll
                    // leak memory and kittens will die. 

                    

                    using (var parser = new DemoParser(fileStream))
                    {
                        // So now we've initialized a demo-parser. 
                        // let's parse the head of the demo-file to get which map the match is on!
                        // this is always the first step you need to do.
                        parser.ParseHeader();

                        // and now, do some magic: grab the match!
                        string map = parser.Map;

                        // Cool! Now let's get started generating the analysis-data. 

                        //Let's just declare some stuff we need to remember

                        // Here we'll save how far a player has travelled each round. 
                        // Here we remember wheter the match has started yet. 
                        bool hasMatchStarted = false;

                        int ctStartroundMoney = 0, tStartroundMoney = 0, ctEquipValue = 0, tEquipValue = 0, ctSaveAmount = 0, tSaveAmount = 0;

                        float ctWay = 0, tWay = 0;

                        int defuses = 0;
                        int plants = 0;


                        Dictionary<Player, int> killsThisRound = new Dictionary<Player, int>();

                        List<Player> ingame = new List<Player>();

                        // Since most of the parsing is done via "Events" in CS:GO, we need to use them. 
                        // So you bind to events in C# as well. 

                        // AFTER we have bound the events, we start the parser!

                        int totalRounds = 0;
                        parser.MatchStarted += (sender, e) =>
                        {
                            hasMatchStarted = true;

                            // Okay, problem: At the end of the demo
                            // a player might have already left the game,
                            // so we need to store some information
                            // about the players before they left :)
                            ingame.AddRange(parser.PlayingParticipants);
                        };

                        parser.PlayerKilled += (object sender, PlayerKilledEventArgs e) =>
                        {
                            //the killer is null if you're killed by the world - eg. by falling
                            if (e.Killer != null)
                            {
                                if (!killsThisRound.ContainsKey(e.Killer))
                                    killsThisRound[e.Killer] = 0;

                                if(OurSteamID(e.Killer.SteamID) && e.Headshot)
                                {
                                    OurSteamIDs[e.Killer.SteamID].Headshots++;
                                }
                                if(e.Victim != null && OurSteamID(e.Victim.SteamID) && OurSteamID(e.Killer.SteamID))
                                {
                                    OurSteamIDs[e.Killer.SteamID].TeamKills++;
                                }

                                //Remember how many kills each player made this rounds
                                killsThisRound[e.Killer]++;
                            }
                        };

                        parser.RoundStart += (sender, e) =>
                        {
                            if (!hasMatchStarted)
                                return;

                            //How much money had each team at the start of the round?
                            ctStartroundMoney = parser.Participants.Where(a => a.Team == Team.CounterTerrorist).Sum(a => a.Money);
                            tStartroundMoney = parser.Participants.Where(a => a.Team == Team.Terrorist).Sum(a => a.Money);

                            //And how much they did they save from the last round?
                            ctSaveAmount = parser.Participants.Where(a => a.Team == Team.CounterTerrorist && a.IsAlive).Sum(a => a.CurrentEquipmentValue);
                            tSaveAmount = parser.Participants.Where(a => a.Team == Team.Terrorist && a.IsAlive).Sum(a => a.CurrentEquipmentValue);

                            //And let's reset those statistics
                            ctWay = 0; tWay = 0;
                            plants = 0; defuses = 0;

                            killsThisRound.Clear();
                        };

                        parser.FreezetimeEnded += (sender, e) =>
                        {
                            if (!hasMatchStarted)
                                return;

                            // At the end of the freezetime (when players can start walking)
                            // calculate the equipment value of each team!
                            ctEquipValue = parser.Participants.Where(a => a.Team == Team.CounterTerrorist).Sum(a => a.CurrentEquipmentValue);
                            tEquipValue = parser.Participants.Where(a => a.Team == Team.Terrorist).Sum(a => a.CurrentEquipmentValue);
                        };

                        parser.BombPlanted += (sender, e) =>
                        {
                            if (!hasMatchStarted)
                                return;
                            if (OurSteamID(e.Player.SteamID))
                                OurSteamIDs[e.Player.SteamID].BombPlants++;

                            plants++;
                        };

                        parser.BombDefused += (sender, e) =>
                        {
                            if (!hasMatchStarted)
                                return;

                            if (OurSteamID(e.Player.SteamID))
                                OurSteamIDs[e.Player.SteamID].BombDefuses++;

                            defuses++;
                        };
                        parser.TickDone += (sender, e) =>
                        {
                            if (!hasMatchStarted)
                                return;

                            // Okay, let's measure how far each team travelled. 
                            // As you might know from school the amount walked
                            // by a player is the sum of it's velocities

                            foreach (var player in parser.PlayingParticipants)
                            {
                                // We multiply it by the time of one tick
                                // Since the velocity is given in 
                                // ingame-units per second
                                float currentWay = (float)(player.Velocity.Absolute * parser.TickTime);

                                // This is just an example of what kind of stuff you can do
                                // with this parser. 
                                // Of course you could find out who makes the most footsteps, and find out
                                // which player ninjas the most - just to give you an example

                                if (player.Team == Team.CounterTerrorist)
                                    ctWay += currentWay;
                                else if (player.Team == Team.Terrorist)
                                    tWay += currentWay;
                            }
                        };

                        //So now lets do some fancy output
                        parser.RoundEnd += (sender, e) =>
                        {
                            if (!hasMatchStarted)
                                return;
                            totalRounds++;
                            //Warmup dont count
                            if (totalRounds > 1)
                            {
                                foreach (var player in parser.PlayingParticipants)
                                    if (OurSteamIDs.ContainsKey(player.SteamID))
                                    {
                                        if (killsThisRound.Any(x => x.Key.SteamID == player.SteamID))
                                        {
                                            if (killsThisRound.ElementAt(0).Key.SteamID == player.SteamID)
                                                OurSteamIDs[player.SteamID].EntryFrags++;
                                            if (killsThisRound.ElementAt(killsThisRound.Count - 1).Key.SteamID == player.SteamID)
                                                OurSteamIDs[player.SteamID].FinalKills++;
                                            int kills = killsThisRound.First(x => x.Key.SteamID == player.SteamID).Value;
                                            switch (kills)
                                            {
                                                case 1:
                                                    OurSteamIDs[player.SteamID].OneKs++;
                                                    break;
                                                case 2:
                                                    OurSteamIDs[player.SteamID].Twoks++;
                                                    break;
                                                case 3:
                                                    OurSteamIDs[player.SteamID].Threeks++;
                                                    break;
                                                case 4:
                                                    OurSteamIDs[player.SteamID].Fourks++;
                                                    break;
                                                case 5:
                                                    OurSteamIDs[player.SteamID].Aces++;
                                                    break;
                                            }
                                        }
                                        else
                                            OurSteamIDs[player.SteamID].NoKills++;
                                    }
                            }
                            
                            // We do this in a method-call since we'd else need to duplicate code
                            // The much parameters are there because I simply extracted a method
                            // Sorry for this - you should be able to read it anywys :)
                           // PrintRoundResults(parser, outputStream, ctStartroundMoney, tStartroundMoney, ctEquipValue, tEquipValue, ctSaveAmount, tSaveAmount, ctWay, tWay, defuses, plants, killsThisRound);
                        };

                        //Now let's parse the demo!
                        parser.ParseToEnd();

                        //And output the result of the last round again. 
                        //PrintRoundResults(parser, outputStream, ctStartroundMoney, tStartroundMoney, ctEquipValue, tEquipValue, ctSaveAmount, tSaveAmount, ctWay, tWay, defuses, plants, killsThisRound);



                        foreach (var player in parser.PlayingParticipants)
                            if (OurSteamIDs.ContainsKey(player.SteamID))
                            {
                                PlayerData currentPlayer;
                                OurSteamIDs.TryGetValue(player.SteamID, out currentPlayer);
                                currentPlayer.Kills = player.AdditionaInformations.Kills;
                                currentPlayer.Deaths = player.AdditionaInformations.Deaths;
                                currentPlayer.Assists = player.AdditionaInformations.Assists;
                                currentPlayer.MVPs = player.AdditionaInformations.MVPs;
                                currentPlayer.Score = player.AdditionaInformations.Score;
                                currentPlayer.Map = parser.Map;
                                
                                currentPlayer.TotalRoundsPlayed = totalRounds;


                                if ((parser.TScore > parser.CTScore && player.Team == Team.Terrorist) || parser.TScore < parser.CTScore && player.Team == Team.CounterTerrorist)
                                {
                                    currentPlayer.WinLossDraw = "W";
                                    currentPlayer.RoundsWon = 16;
                                    currentPlayer.RoundsLost = currentPlayer.TotalRoundsPlayed - 16;
                                }
                                else if (parser.TScore == parser.CTScore)
                                {
                                    currentPlayer.WinLossDraw = "D";
                                    currentPlayer.RoundsWon = 15;
                                    currentPlayer.RoundsLost = 15;
                                }
                                else
                                {
                                    currentPlayer.WinLossDraw = "L";
                                    currentPlayer.RoundsLost = 16;
                                    currentPlayer.RoundsWon = currentPlayer.TotalRoundsPlayed - 16;
                                }
                                currentPlayer.MoneySpent = player.AdditionaInformations.TotalCashSpent;

                                currentPlayer.MatchID = fileName.Substring(fileName.LastIndexOf('\\') + 1); ;
                              
                                
                                
                                

                            }
                    }

                

               
                }
                
            }
        }
    }
}
