using System.Collections.Generic;
using PluginContracts;
using PluginLoader;
using System.IO;
using System.Xml.Serialization;
using System.Security.Cryptography;
using System;
using System.Linq;
using System.IO.Compression;
using System.Reflection;
using System.Diagnostics;
using System.Net;

namespace Blueberries
{
    class blueberries
    {

        const string strongLine = "===================================================";
        const string baseURL = "https://www.pancakes.wtf/butter/";
        const string dlFolder = "Archives";
        const ConsoleColor workingColor = ConsoleColor.Green;
        const ConsoleColor neutralColor = ConsoleColor.White;
        const ConsoleColor errColor = ConsoleColor.Red;

        static void Main(string[] args)
        {

            Console.OutputEncoding = System.Text.Encoding.UTF8;
            

            Introduction();

            CleanDLDir();

            Console.ForegroundColor = workingColor;

            //get index of installed packages
            Console.WriteLine(" Indexing your installed packages...");
            SerializableDictionary<string, string> installedPackages = RunPlugIndex();
            Console.WriteLine("");
            Console.WriteLine("\tFound " + installedPackages.Keys.Count.ToString() + " packages.");
            PrintStrongLine();



            //download package list
            Console.WriteLine(" Downloading latest package list...");
            SerializableDictionary<string, SerializableDictionary<string, string>> availablePackages = GetPackageList();
            Console.WriteLine("\tFinished.");
            PrintStrongLine();

            
            //check for differences
            Console.WriteLine(" Checking differences between installed and available...");
            Dictionary<string, string[]> diffs = CheckDiffs(installedPackages, availablePackages);
            PrintStrongLine();


            //download packages that need to be installed
            Console.WriteLine(" Downloading package updates...");
            List<string> approvedToInstall = DownloadUpdates(diffs, availablePackages);
            PrintStrongLine();

            //extract archives
            Console.WriteLine(" Installing updates...");
            InstallUpdates();
            PrintStrongLine();
            
            
            CleanDLDir();

            PrintStrongLine();
            Console.WriteLine(" All finished! Press the [Enter] key to close...");
            Console.ReadLine();

        }





        static string CalculateMD5(string filename)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(filename))
                {
                    var hash = md5.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
        }

        static void CleanExit()
        {
            PrintStrongLine();
            Console.WriteLine(" This application will now close.");
            Console.WriteLine(" Press the [Enter] key to continue.");
            Console.ReadLine();
            Environment.Exit(0);
        }
        static void Introduction()
        {
            PrintStrongLine();
            Console.BackgroundColor = ConsoleColor.Blue;
            Console.ForegroundColor = neutralColor;
            Console.WriteLine(" .--. .           .                               ");
            Console.WriteLine(" |   )|           |                   o           ");
            Console.WriteLine(" |--: | .  .  .-. |.-.  .-. .--..--.  .   .-. .--.");
            Console.WriteLine(" |   )| |  | (.-' |   )(.-' |   |     |  (.-' `--.");
            Console.WriteLine(" '--' `-`--`- `--''`-'  `--''   '   -' `- `--'`--'");
            Console.WriteLine("");
            Console.WriteLine(" Blueberries (v" + Assembly.GetExecutingAssembly().GetName().Version.ToString() + ")");
            Console.WriteLine(" (c) 2020-present, Ryan L. Boyd, Ph.D.");
            Console.BackgroundColor = ConsoleColor.Black;
            PrintStrongLine();

            Console.WriteLine(" Welcome to Blueberries!");
            Console.WriteLine(" Blueberries is the official plugin updater for BUTTER. This software will scan");
            Console.WriteLine(" your installed plugins, then check to see if newer versions are available.");
            Console.WriteLine(" If newer versions are found, they will be downloaded and installed.");
            Console.WriteLine("");
            Console.WriteLine(" THE SOFTWARE IS PROVIDED \"AS IS\", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,");
            Console.WriteLine(" INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A ");
            Console.WriteLine(" PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT");
            Console.WriteLine(" HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF");
            Console.WriteLine(" CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE");
            Console.WriteLine(" OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.");
            PrintStrongLine();


            PermissionToContinue();
            return;
        }

        static void CleanDLDir()
        {
            try { 
                string archiveDir = dlFolder;
                var dirToDel = new DirectoryInfo(archiveDir);
                if (dirToDel.Exists) Directory.Delete(archiveDir, true);
                while (Directory.Exists(archiveDir))
                {
                    System.Threading.Thread.Sleep(100);
                    dirToDel.Refresh();
                }

                Directory.CreateDirectory(archiveDir);
                while (!Directory.Exists(archiveDir))
                {
                    System.Threading.Thread.Sleep(100);
                    dirToDel.Refresh();
                }
            }
            catch (Exception error)
            {
                PrintStrongLine();
                Console.ForegroundColor = errColor;
                Console.WriteLine(" ERROR: Unable to create a clean \"Archives\\\" folder for downloads.");
                Console.WriteLine(" Please try again or, alternatively, you can manually delete this folder.");
                Console.ForegroundColor = workingColor;
                CleanExit();
            }
        }

        static void DeleteFile(string fileName)
        {
            try { 
                var fileToDel = new FileInfo(fileName);
                if (fileToDel.Exists) File.Delete(fileName);
                while (File.Exists(fileName))
                {
                    System.Threading.Thread.Sleep(100);
                    fileToDel.Refresh();
                }
            }
            catch (Exception error)
            {
                Console.ForegroundColor = errColor;
                Console.Write("\tCould not delete file:");
                Console.Write("\t" + fileName);
                Console.ForegroundColor = workingColor;
            }
        }

        static void PrintStrongLine()
        {
            Console.WriteLine("");
            Console.WriteLine(strongLine);
            Console.WriteLine("");
            return;
        }

        static void PermissionToContinue()
        {
            bool confirmed = false;
            bool denied = false;
            string Key;
            do
            {
                ConsoleKey response;
                do
                {
                    Console.Write(" Do you want to update your plugins? (y/n): ");
                    response = Console.ReadKey(false).Key;
                    if (response != ConsoleKey.Enter)
                        Console.WriteLine();

                } while (response != ConsoleKey.Y && response != ConsoleKey.N);

                confirmed = response == ConsoleKey.Y;
                denied = response == ConsoleKey.N;
            } while (!confirmed && !denied);

            if (denied) 
            {
                Console.WriteLine("");
                Console.WriteLine(" Exiting now...");
                System.Threading.Thread.Sleep(1500);
                Environment.Exit(0);
            }

            return;
            
        }
        static SerializableDictionary<string, string> RunPlugIndex()
        {
            var p = new Process();
            p.StartInfo = new ProcessStartInfo("PlugIndex.exe")
            {
                UseShellExecute = false
            };

            p.Start();
            p.WaitForExit();

            SerializableDictionary<string, string> installedPlugins;
            using (Stream reader = new FileStream("BUTTER-Installed-Plugins.xml", FileMode.Open))
            {
                XmlSerializer serializerInput = new XmlSerializer(typeof(SerializableDictionary<string, string>));
                installedPlugins = (SerializableDictionary<string, string>)serializerInput.Deserialize(reader);
            }

            return (installedPlugins);

        }
        static SerializableDictionary<string, SerializableDictionary<string, string>> GetPackageList()
        {

            SerializableDictionary<string, SerializableDictionary<string, string>> availablePlugins = new SerializableDictionary<string, SerializableDictionary<string, string>>();

            WebClient client = new WebClient();
            try
            {
                using (Stream stream = client.OpenRead(baseURL + "BUTTER-Plugin-Dat.xml"))
                {
                    XmlSerializer serializerInput = new XmlSerializer(typeof(SerializableDictionary<string, SerializableDictionary<string, string>>));
                    StreamReader reader = new StreamReader(stream);
                    availablePlugins = (SerializableDictionary<string, SerializableDictionary<string, string>>)serializerInput.Deserialize(reader);
                }
            }
            catch
            {
                //do nothing if it can't connect
            }

            if (availablePlugins.Keys.Count == 0)
            {
                Console.WriteLine(" Unable to download package list.");
                Console.WriteLine(" Are you not connected to the internet?");
                CleanExit();
            }

            return (availablePlugins);

        }
        static Dictionary<string, string[]> CheckDiffs(SerializableDictionary<string, string> installed, SerializableDictionary<string, SerializableDictionary<string, string>> available)
        {

            Dictionary<string, string[]> diffs = new Dictionary<string, string[]>();

            foreach (var availItem in available)
            {

                string key = availItem.Key;
                SerializableDictionary<string, string> availplug = availItem.Value;

                if (installed.ContainsKey(availplug["Name"]))
                {
                    if (installed[availplug["Name"]] != available[availplug["Name"]]["Version"]) diffs.Add(availplug["Name"],
                                                                                                    new string[] { installed[availplug["Name"]], available[availplug["Name"]]["Version"] });
                }
                else
                {
                    diffs.Add(availplug["Name"], new string[] { "n/a", availplug["Version"] });
                }
            }

            if (diffs.Keys.Count == 0)
            {
                Console.WriteLine("");
                Console.WriteLine("\tAll of your plugins are already up to date.");
                Console.WriteLine("\tNo further action will be taken at this time.");
                Console.WriteLine("");
                Console.WriteLine("\tThis application will now close.");
                Console.WriteLine("\tPress the [Enter] key to continue.");
                Console.ReadLine();
                Environment.Exit(0);
            }
            else
            {
                Console.WriteLine("");
                Console.WriteLine("\tYou have " + diffs.Keys.Count.ToString() + " plugins that need to be updated.");
            }

            return (diffs);

        }


        static double ProgPercent;
        static List<string> DownloadUpdates(Dictionary<string, string[]> diffs, SerializableDictionary<string, SerializableDictionary<string, string>> availablePackages)
        {

            List<string> approvedToInstall = new List<string>();

            //create out download dir if it doesn't exist
            var dlDir = new DirectoryInfo(dlFolder);
            if (!dlDir.Exists) dlDir.Create();
            while (!Directory.Exists(dlDir.FullName.ToString()))
            {
                System.Threading.Thread.Sleep(100);
                dlDir.Refresh();
            }

            foreach (string plugin in diffs.Keys)
            {
                ProgPercent = 0;
                Console.WriteLine("\tDownloading '" + plugin + "' (" + availablePackages[plugin]["Version"] + ")" );

                string fileName = availablePackages[plugin]["File"];
                Uri fileURL = new Uri(baseURL + fileName);

                try { 
                    using (WebClient downloader = new WebClient())
                    {
                        //console progress bar
                        using (var progress = new ProgressBar())
                        {

                            downloader.DownloadProgressChanged += UpdateBytesRec;
                            downloader.DownloadFileTaskAsync(fileURL, fileName);

                            while (downloader.IsBusy)
                            {
                                progress.Report(ProgPercent / (double)100.0);
                            }
                            progress.Report(1.0);
                            System.Threading.Thread.Sleep(100);

                        }
                    }

                    Console.Write("\t\tValidating MD5 checksum... ");
                    string md5 = CalculateMD5(fileName);

                    if (md5 == availablePackages[plugin]["MD5checksum"])
                    {
                        approvedToInstall.Add(fileName);
                        Console.WriteLine("OK.");
                    }
                    else
                    {
                        Console.ForegroundColor = errColor;
                        Console.WriteLine("\t\tERROR: MD5 checksums do not match.");
                        Console.WriteLine("\t\tPlease notify the software's author.");
                        Console.ForegroundColor = workingColor;
                        DeleteFile(fileName);
                        System.Threading.Thread.Sleep(1000);
                    }

                }
                catch (Exception error)
                {
                    //if there was any issue whatsoever, then don't add to list of things to install
                    //and, additionally, delete whatever was downloaded thus far
                    Console.ForegroundColor = errColor;
                    Console.WriteLine("\t\tERROR: There was an error downloading\\validating this package.");
                    Console.WriteLine("\t\tPlease notify the software's author if this issue persists.");
                    Console.ForegroundColor = workingColor;
                    DeleteFile(fileName);
                }



            }

            return(approvedToInstall);
        }
        // Event to track the progress
        static void UpdateBytesRec(object sender, DownloadProgressChangedEventArgs e)
        {
            ProgPercent = e.ProgressPercentage;
        }


        static void InstallUpdates()
        {

            //first thing, create a backup folder
            try
            {
                string backupDir = dlFolder + "/Backups";
                var dirToCreate = new DirectoryInfo(backupDir);
                Directory.CreateDirectory(backupDir);
                while (!Directory.Exists(backupDir))
                {
                    System.Threading.Thread.Sleep(100);
                    dirToCreate.Refresh();
                }
            }
            catch (Exception error)
            {
                PrintStrongLine();
                Console.ForegroundColor = errColor;
                Console.WriteLine(" ERROR: Unable to create a clean \"Archives\\Backups\\\" folder.");
                Console.WriteLine(" Please try again or, alternatively, you can manually extract your updates.");
                Console.ForegroundColor = workingColor;
                CleanExit();

            }


            //get list of all the zips
            List<string> zipfiles = Directory.EnumerateFiles(dlFolder + "/", "*.*",
                                              SearchOption.TopDirectoryOnly)
                       .Where(n => Path.GetExtension(n) == ".zip").ToList();

            Dictionary<string, List<string[]>> backups = CreateBackups(zipfiles);

            ExtractArchives(zipfiles, backups);
            
        }


        static Dictionary<string, List<string[]>> CreateBackups(List<string> zipfiles)
        {

            Console.WriteLine("\tCreating Backups...");

            Dictionary<string, List<string[]>> backups = new Dictionary<string, List<string[]>>();

            foreach (string file in zipfiles)
            {

                using (ZipArchive zip = ZipFile.OpenRead(file)) { 
                    foreach (ZipArchiveEntry entry in zip.Entries)
                    {

                        string dirToCreate = entry.FullName;
                        if (entry.FullName.Contains(entry.Name) && !String.IsNullOrEmpty(entry.Name)) dirToCreate = dlFolder + "/Backups/" + entry.FullName.Replace(entry.Name, "");
                        Directory.CreateDirectory(dirToCreate);

                        //if the entry is a file
                        if (entry.Name != "")
                        {
                            if (File.Exists(entry.FullName) && !File.Exists(dlFolder + "/Backups/" + entry.FullName))
                            {
                                File.Copy(entry.FullName, dlFolder + "/Backups/" + entry.FullName);
                                if (!backups.ContainsKey(file)) backups.Add(file, new List<string[]>());
                                backups[file].Add(new string[] { entry.FullName, dlFolder + "/Backups/" + entry.FullName });
                                
                                
                                DeleteFile(entry.FullName);
                            }
                        }
                    }

                }
            }

            return (backups);


        }

        static void ExtractArchives(List<string> zipfiles, Dictionary<string, List<string[]>> backups)
        {
            foreach (string file in zipfiles)
            {

                Console.WriteLine("\tExtracting " + file);

                try { 
                    using (ZipArchive zip = ZipFile.OpenRead(file)) { 
                        foreach (ZipArchiveEntry entry in zip.Entries)
                        {

                            string dirToCreate = entry.FullName;
                            if (entry.FullName.Contains(entry.Name) && !String.IsNullOrEmpty(entry.Name)) dirToCreate = entry.FullName.Replace(entry.Name, "");
                            Directory.CreateDirectory(dirToCreate);

                            //if the entry is a file
                            if (entry.Name != "") entry.ExtractToFile(entry.FullName, true);

                        }
                    }
                }
                catch
                {
                    Console.ForegroundColor = errColor;
                    Console.WriteLine("\t\tThere was an error extracting " + file);
                    Console.WriteLine("Restoring backup for this plugin...");
                    Console.ForegroundColor = workingColor;
                    foreach (string[] files in backups[file])
                    {
                        File.Copy(files[1], files[0], true);
                    }
                }
            }
        }
        



    }




}


