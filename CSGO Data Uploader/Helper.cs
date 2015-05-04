#region

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;

#endregion

namespace Hearthstone_Deck_Tracker
{
	public static class Helper
	{
		public static double DpiScalingX = 1.0, DpiScalingY = 1.0;

		public static readonly Dictionary<string, string> LanguageDict = new Dictionary<string, string>
		{
			{"English", "enUS"},
			{"Chinese (China)", "zhCN"},
			{"Chinese (Taiwan)", "zhTW"},
			{"English (Great Britain)", "enGB"},
			{"French", "frFR"},
			{"German", "deDE"},
			{"Italian", "itIT"},
			{"Korean", "koKR"},
			{"Polish", "plPL"},
			{"Portuguese (Brazil)", "ptBR"},
			{"Portuguese (Portugal)", "ptPT"},
			{"Russian", "ruRU"},
			{"Spanish (Mexico)", "esMX"},
			{"Spanish (Spain)", "esES"}
		};

		public static Version CheckForUpdates(out Version newVersionOut)
		{
			
			newVersionOut = null;

			const string versionXmlUrl =
				@"https://raw.githubusercontent.com/Epix37/Hearthstone-Deck-Tracker/master/Hearthstone%20Deck%20Tracker/Version.xml";

			var currentVersion = GetCurrentVersion();

			if(currentVersion != null)
			{
				try
				{
					var xml = new WebClient {Proxy = null}.DownloadString(versionXmlUrl);

					var newVersion = new Version(XmlManager<SerializableVersion>.LoadFromString(xml).ToString());

					if(newVersion > currentVersion)
						newVersionOut = newVersion;
				}
				catch(Exception e)
				{
					MessageBox.Show("Error checking for new version.\n\n" + e.Message + "\n\n" + e.InnerException);
				}
			}

			return currentVersion;
		}

		// A bug in the SerializableVersion.ToString() method causes this to load Version.xml incorrectly.
		// The build and revision numbers are swapped (i.e. a Revision of 21 in Version.xml loads to Version.Build == 21).
		public static Version GetCurrentVersion()
		{
			try
			{
				return new Version(XmlManager<SerializableVersion>.Load("Version.xml").ToString());
			}
			catch(Exception e)
			{
				MessageBox.Show(
				                e.Message + "\n\n" + e.InnerException
				                + "\n\n If you don't know how to fix this, please overwrite Version.xml with the default file.",
				                "Error loading Version.xml");

				return null;
			}
		}

		public static bool IsNumeric(char c)
		{
			int output;
			return Int32.TryParse(c.ToString(), out output);
		}

		public static bool IsHex(IEnumerable<char> chars)
		{
			return chars.All(c => ((c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F')));
		}

		public static double DrawProbability(int copies, int deck, int draw)
		{
			return 1 - (BinomialCoefficient(deck - copies, draw) / BinomialCoefficient(deck, draw));
		}

		public static double BinomialCoefficient(int n, int k)
		{
			double result = 1;
			for(var i = 1; i <= k; i++)
			{
				result *= n - (k - i);
				result /= i;
			}
			return result;
		}

		public static string GetValidFilePath(string dir, string name, string extension)
		{
			var validDir = RemoveInvalidPathChars(dir);
			if(!Directory.Exists(validDir))
				Directory.CreateDirectory(validDir);

			if(!extension.StartsWith("."))
				extension = "." + extension;

			var path = validDir + "\\" + RemoveInvalidFileNameChars(name);
			if(File.Exists(path + extension))
			{
				var num = 1;
				while(File.Exists(path + "_" + num + extension))
					num++;
				path += "_" + num;
			}

			return path + extension;
		}

		public static string RemoveInvalidPathChars(string s)
		{
			var invalidChars = new string(Path.GetInvalidPathChars());
			var regex = new Regex(string.Format("[{0}]", Regex.Escape(invalidChars)));
			return regex.Replace(s, "");
		}

		public static string RemoveInvalidFileNameChars(string s)
		{
			var invalidChars = new string(Path.GetInvalidFileNameChars());
			var regex = new Regex(string.Format("[{0}]", Regex.Escape(invalidChars)));
			return regex.Replace(s, "");
		}

		public static void SortCardCollection(IEnumerable collection, bool classFirst)
		{
			if(collection == null)
				return;
			var view1 = (CollectionView)CollectionViewSource.GetDefaultView(collection);
			view1.SortDescriptions.Clear();

			if(classFirst)
				view1.SortDescriptions.Add(new SortDescription("IsClassCard", ListSortDirection.Descending));

			view1.SortDescriptions.Add(new SortDescription("Cost", ListSortDirection.Ascending));
			view1.SortDescriptions.Add(new SortDescription("Type", ListSortDirection.Descending));
			view1.SortDescriptions.Add(new SortDescription("LocalizedName", ListSortDirection.Ascending));
		}


		private static bool IsYellowPixel(Color pixel)
		{
			const int red = 216;
			const int green = 174;
			const int blue = 10;
			const int deviation = 10;
			return Math.Abs(pixel.R - red) <= deviation && Math.Abs(pixel.G - green) <= deviation && Math.Abs(pixel.B - blue) <= deviation;
		}

		//http://stackoverflow.com/questions/23927702/move-a-folder-from-one-drive-to-another-in-c-sharp
		public static void CopyFolder(string sourceFolder, string destFolder)
		{
			if(!Directory.Exists(destFolder))
				Directory.CreateDirectory(destFolder);
			var files = Directory.GetFiles(sourceFolder);
			foreach(var file in files)
			{
				var name = Path.GetFileName(file);
				var dest = Path.Combine(destFolder, name);
				File.Copy(file, dest);
			}
			var folders = Directory.GetDirectories(sourceFolder);
			foreach(var folder in folders)
			{
				var name = Path.GetFileName(folder);
				var dest = Path.Combine(destFolder, name);
				CopyFolder(folder, dest);
			}
		}


		//http://stackoverflow.com/questions/3769457/how-can-i-remove-accents-on-a-string
		public static string RemoveDiacritics(string src, bool compatNorm)
		{
			var sb = new StringBuilder();
			foreach(var c in src.Normalize(compatNorm ? NormalizationForm.FormKD : NormalizationForm.FormD))
			{
				switch(CharUnicodeInfo.GetUnicodeCategory(c))
				{
					case UnicodeCategory.NonSpacingMark:
					case UnicodeCategory.SpacingCombiningMark:
					case UnicodeCategory.EnclosingMark:
						break;
					default:
						sb.Append(c);
						break;
				}
			}

			return sb.ToString();
		}

		public static string GetWinPercentString(int wins, int losses)
		{
			if(wins + losses == 0)
				return "-%";
			return Math.Round(wins * 100.0 / (wins + losses), 0) + "%";
		}

		public static T DeepClone<T>(T obj)
		{
			using(var ms = new MemoryStream())
			{
				var formatter = new BinaryFormatter();
				formatter.Serialize(ms, obj);
				ms.Position = 0;

				return (T)formatter.Deserialize(ms);
			}
		}
	}
}