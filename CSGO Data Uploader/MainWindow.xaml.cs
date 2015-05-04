using Google.GData.Client;
using Google.GData.Spreadsheets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace CSGO_Data_Uploader
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        GoogleSpreadsheet g = new GoogleSpreadsheet();
        public MainWindow()
        {
            InitializeComponent();
        }

        private async void Add(object sender, RoutedEventArgs e)
        {
           await Task.Run( () => ParseGame());
        }

        private void ParseGame()
        {
            string path = "C:\\Program Files (x86)\\SteamLibrary\\steamapps\\common\\Counter-Strike Global Offensive\\csgo\\replays";
            string[] files = new string[100];
            try
            {
                files = System.IO.Directory.GetFiles(path, "*.dem");
            }
            catch
            {
                try
                {
                    path = "C:\\Program Files (x86)\\Steam\\SteamApps\\common\\Counter-Strike Global Offensive\\csgo\\replays";
                    files = System.IO.Directory.GetFiles(path, "*.dem");
                }
                catch
                {

                }
            }
            List<string> matchIds = g.GetColumnData("matchid").Distinct().ToList();
            int filesProcessed = 0, filesSkipped = 0;
            //  files = files.Take(2).ToArray();
            // Every argument is a file, so let's iterate over all the arguments
            // So you can call this program like
            // > StatisticsGenerator.exe hello.dem bye.dem
            // It'll generate the statistics.
            foreach (var fileName in files)
            {
               this.Dispatcher.Invoke((Action)(() =>  Output.Content = "Processed " + (filesProcessed + filesSkipped) + " of " + (files.Length-1) + "\n" +
                                 "Matches Added : " + filesProcessed + "\n" +
                                 "Matches Skipped : " + filesSkipped ));

               this.Dispatcher.Invoke((Action)(() => LoadingBar.Value = ((float)(filesSkipped + filesProcessed) / (files.Length - 1)) * 100));
                if (matchIds.Any(x => x == fileName.Substring(fileName.LastIndexOf('\\') + 1)))
                {
                    filesSkipped++;
                    continue;
                }
                filesProcessed++;
                Parser parser = new Parser();

                parser.ParseGame(fileName);

                AddDataToSheet(parser);
            }
        }

        private void AddDataToSheet(Parser parser)
        {
            foreach (KeyValuePair<long, PlayerData> player in parser.OurSteamIDs)
            {
                if (player.Value.TotalRoundsPlayed > 0)
                {
                    g.AddGameRecord(player.Value);
                }
            }
        }
    }
    public class GoogleSpreadsheet
    {
        private SpreadsheetsService service;
        List<string> columnNumber;
        int columnNum = 0;
        //*******************************************
        //* OAUTH VARIABLES
        //*******************************************
        private OAuth2Parameters oAuthParams;

        // OAuth2.0 info.
        private const string CLIENT_ID = "362188191615-hj79k5tppvtha98aq9a81lv1q2ujae0h.apps.googleusercontent.com";
        private const string CLIENT_SECRET = "japg7YjQ1PkdvK6OXbNzM74N";
        private const string REDIRECT_URI = "urn:ietf:wg:oauth:2.0:oob";
        private const string SCOPE = "https://spreadsheets.google.com/feeds/ https://docs.google.com/feeds/";
        public GoogleSpreadsheet()
        {
            Authenticate();
            
        }
        private void Authenticate()
        {
            // Set login info.
            service = new SpreadsheetsService("UnityConnect");
            // Create OAuth2 Parameters.
            oAuthParams = new OAuth2Parameters();
            oAuthParams.ClientId = CLIENT_ID;
            oAuthParams.ClientSecret = CLIENT_SECRET;
            oAuthParams.RedirectUri = REDIRECT_URI;
            oAuthParams.Scope = SCOPE;
            oAuthParams.RefreshToken = "1/3fZbFvN3iFmwkmoFRgmFSbq-0oeRQRdB_urTYhA_3d990RDknAdJa_sgfheVM0XT";
            oAuthParams.AccessType = "offline";

            OAuthUtil.RefreshAccessToken(oAuthParams);
            GOAuth2RequestFactory requestFactory = new GOAuth2RequestFactory(null, "UnityConnect", oAuthParams);
            service.RequestFactory = requestFactory;

        }
        public void AddGameRecord(PlayerData player)
        {
            if (columnNumber == null)
            {
               columnNumber = GetColumnData("rownum");
               int.TryParse(columnNumber.Last(), out columnNum);
               columnNum++;
            }
            else
                columnNum++;
            // Instantiate a SpreadsheetQuery object to retrieve spreadsheets.
            SpreadsheetQuery query = new SpreadsheetQuery();

            // Make a request to the API and get all spreadsheets.
            SpreadsheetFeed feed = service.Query(query);

            if (feed.Entries.Count == 0)
            {
                // TODO: There were no spreadsheets, act accordingly.
            }

            // TODO: Choose a spreadsheet more intelligently based on your
            // app's needs.
            SpreadsheetEntry spreadsheet = (SpreadsheetEntry)feed.Entries[0];
            Console.WriteLine(spreadsheet.Title.Text);

            // Get the first worksheet of the first spreadsheet.
            // TODO: Choose a worksheet more intelligently based on your
            // app's needs.
            WorksheetFeed wsFeed = spreadsheet.Worksheets;
            WorksheetEntry worksheet = (WorksheetEntry)wsFeed.Entries.First(x => x.Title.Text == "RAW_DATA");

            // Define the URL to request the list feed of the worksheet.
            AtomLink listFeedLink = worksheet.Links.FindService(GDataSpreadsheetsNameTable.ListRel, null);

            // Fetch the list feed of the worksheet.
            ListQuery listQuery = new ListQuery(listFeedLink.HRef.ToString());
            ListFeed listFeed = service.Query(listQuery);


            
            // Create a local representation of the new row.
            ListEntry row = new ListEntry();
            row.Elements.Add(new ListEntry.Custom() { LocalName = "steamid", Value = player.SteamID.ToString() });
            row.Elements.Add(new ListEntry.Custom() { LocalName = "name", Value = player.Name });
            row.Elements.Add(new ListEntry.Custom() { LocalName = "matchid", Value = player.MatchID });
            row.Elements.Add(new ListEntry.Custom() { LocalName = "map", Value = player.Map });
            row.Elements.Add(new ListEntry.Custom() { LocalName = "totalroundsplayed", Value = player.TotalRoundsPlayed.ToString() });
            row.Elements.Add(new ListEntry.Custom() { LocalName = "kills", Value = player.Kills.ToString() });
            row.Elements.Add(new ListEntry.Custom() { LocalName = "deaths", Value = player.Deaths.ToString() });
            row.Elements.Add(new ListEntry.Custom() { LocalName = "assists", Value = player.Assists.ToString() });
            row.Elements.Add(new ListEntry.Custom() { LocalName = "mvps", Value = player.MVPs.ToString() });
            row.Elements.Add(new ListEntry.Custom() { LocalName = "score", Value = player.Score.ToString() });
            row.Elements.Add(new ListEntry.Custom() { LocalName = "winlossdraw", Value = player.WinLossDraw.ToString() });
            row.Elements.Add(new ListEntry.Custom() { LocalName = "bombplants", Value = player.BombPlants.ToString() });
            row.Elements.Add(new ListEntry.Custom() { LocalName = "bombdefuses", Value = player.BombDefuses.ToString() });
            row.Elements.Add(new ListEntry.Custom() { LocalName = "headshots", Value = player.Headshots.ToString() });
            row.Elements.Add(new ListEntry.Custom() { LocalName = "moneyspent", Value = player.MoneySpent.ToString() });
            row.Elements.Add(new ListEntry.Custom() { LocalName = "nokills", Value = player.NoKills.ToString() });
            row.Elements.Add(new ListEntry.Custom() { LocalName = "oneks", Value = player.OneKs.ToString() });
            row.Elements.Add(new ListEntry.Custom() { LocalName = "twoks", Value = player.Twoks.ToString() });
            row.Elements.Add(new ListEntry.Custom() { LocalName = "threeks", Value = player.Threeks.ToString() });
            row.Elements.Add(new ListEntry.Custom() { LocalName = "fourks", Value = player.Fourks.ToString() });
            row.Elements.Add(new ListEntry.Custom() { LocalName = "aces", Value = player.Aces.ToString() });
            row.Elements.Add(new ListEntry.Custom() { LocalName = "firstkills", Value = player.EntryFrags.ToString() });
            row.Elements.Add(new ListEntry.Custom() { LocalName = "lastkills", Value = player.FinalKills.ToString() });
            row.Elements.Add(new ListEntry.Custom() { LocalName = "teamkills", Value = player.TeamKills.ToString() });
            row.Elements.Add(new ListEntry.Custom() { LocalName = "roundswon", Value = player.RoundsWon.ToString() });
            row.Elements.Add(new ListEntry.Custom() { LocalName = "roundslost", Value = player.RoundsLost.ToString() });
            row.Elements.Add(new ListEntry.Custom() { LocalName = "mapweighting", Value = player.WinLossDraw == "W" ? "1" : "0" });
            row.Elements.Add(new ListEntry.Custom() { LocalName = "rownum", Value = columnNum.ToString()});


            // Send the new row to the API for insertion.
            service.Insert(listFeed, row);
        }

        public List<string> GetColumnData(string columnName)
        {
            List<string> matchIds = new List<string>();
            // Instantiate a SpreadsheetQuery object to retrieve spreadsheets.
            SpreadsheetQuery query = new SpreadsheetQuery();

            // Make a request to the API and get all spreadsheets.
            SpreadsheetFeed feed = service.Query(query);

            if (feed.Entries.Count == 0)
            {
                // TODO: There were no spreadsheets, act accordingly.
            }

            // TODO: Choose a spreadsheet more intelligently based on your
            // app's needs.
            SpreadsheetEntry spreadsheet = (SpreadsheetEntry)feed.Entries[0];
            Console.WriteLine(spreadsheet.Title.Text);

            // Get the first worksheet of the first spreadsheet.
            // TODO: Choose a worksheet more intelligently based on your
            // app's needs.
            WorksheetFeed wsFeed = spreadsheet.Worksheets;
            WorksheetEntry worksheet = (WorksheetEntry)wsFeed.Entries.First(x => x.Title.Text == "RAW_DATA");

            // Define the URL to request the list feed of the worksheet.
            AtomLink listFeedLink = worksheet.Links.FindService(GDataSpreadsheetsNameTable.ListRel, null);

            // Fetch the list feed of the worksheet.
            ListQuery listQuery = new ListQuery(listFeedLink.HRef.ToString());

            ListFeed listFeed = service.Query(listQuery);

            // Iterate through each row.
            foreach (ListEntry row in listFeed.Entries)
            {
                //go over each CELL in the row
                foreach (ListEntry.Custom element in row.Elements)
                {
                    //print only the CELLS that there father (xmlName) is "WebsiteList2"
                    if (element.XmlName == columnName)
                    {
                         matchIds.Add(element.Value);
                    }
                }

            }
            return matchIds;

        }
    }

}
