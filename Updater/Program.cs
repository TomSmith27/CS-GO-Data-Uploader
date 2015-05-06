using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Updater
{
	internal class Program
	{
		private const string Url = @"https://github.com//TomSmith27/CS-GO-Data-Uploader/releases/download/{0}/CSGO.Data.Uploader.zip";

		private static void Main(string[] args)
		{
			Console.Title = "CSGO Data Uploader Updater";
			Console.CursorVisible = false;
			if (args.Length != 2)
			{
				Console.WriteLine("Invalid arguments");
				return;
			}
			try
			{
				//wait for tracker to shut down
				Thread.Sleep(1000);

				int procId = int.Parse(args[0]);
				if(Process.GetProcesses().Any(p => p.Id == procId))
				{
					Process.GetProcessById(procId).Kill();
					Console.WriteLine("Killed CSGO Data Uploader process");
				}
			}
			catch
			{
				return;
			}


			var update = Update(args[1]);
			update.Wait();

		}

		private static async Task Update(string version)
		{
			try
			{
				var filePath = string.Format("temp/v{0}.zip", version);

				Console.WriteLine("Creating temp file directory");
				if(Directory.Exists("temp"))
					Directory.Delete("temp", true);
				Directory.CreateDirectory("temp");
				
				using(var wc = new WebClient())
				{
					var lockThis = new object();
					Console.WriteLine("Downloading latest version... 0%");
					wc.DownloadProgressChanged += (sender, e) =>
						{
							lock(lockThis)
							{
								Console.CursorLeft = 0;
								Console.CursorTop = 1;
								Console.WriteLine("Downloading latest version... {0}/{1}KB ({2}%)", e.BytesReceived / (1024), e.TotalBytesToReceive / (1024), e.ProgressPercentage);
							}
						};
					await wc.DownloadFileTaskAsync(string.Format(Url, version), filePath);
				}
				File.Move(filePath, filePath.Replace("rar", "zip"));
				Console.WriteLine("Extracting files...");
				ZipFile.ExtractToDirectory(filePath, "temp");
				string dowloadPath = System.Environment.CurrentDirectory + "\\temp";
                CopyFiles(System.Environment.CurrentDirectory, dowloadPath);
				Console.WriteLine("Cleaning up...");
				Console.WriteLine("Done!");

                Process.Start("CSGO Data Uploader.exe");
			}
			catch (Exception e)
			{
                Console.WriteLine(e.InnerException);
                Console.WriteLine(e.ToString());
            
				Console.WriteLine("There was a problem updating to the latest version. Pressing any key will direct you to the manual download.");
				Console.ReadKey();
                Process.Start(@"https://github.com/TomSmith27/CS-GO-Data-Uploader/releases/");
			}
			finally
			{
				if(Directory.Exists("temp"))
					Directory.Delete("temp", true);
			}
		}

        private static void CopyFiles(string installLocation, string downloadLocation)
        {
            foreach (var file in Directory.GetFiles(downloadLocation))
                {
                    try
                    {
                        if (!file.Contains("Updater"))
                            File.Copy(file, installLocation + "\\" + file.Substring(file.LastIndexOf('\\') + 1), true);
                    }
                    catch { }
                }
        }
		
	}
}