using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArbitrarySteam
{
    class SteamUser
    {
        public bool BadProfile { get; set; }

        public string URL { get; set; }
        public string SteamID { get; set; }
        public string Name { get; set; }
        public string AvatarUrl { get; set; }
        public string GamesString { get; set; }

        public string SteamLocation { get; set; }

        public List<SteamGame> Games { get; set; }

        public static List<SteamGame> GetInstalledGamesList()
        {            
            List<SteamGame> installedGames = new List<SteamGame>();
            List<string> filesInSteamapps;

            if (Properties.Settings.Default.SteamDirectories == null) { return null; }

            foreach(string steamAppsFolder in Properties.Settings.Default.SteamDirectories)
            {
                try
                {
                    filesInSteamapps = System.IO.Directory.GetFiles(steamAppsFolder, "*.acf").ToList<string>();
                }
                catch { return null; }

                foreach (string file in filesInSteamapps)
                {
                    if (file.Contains("appmanifest_"))
                    {
                        string appID = file.Substring(file.IndexOf("appmanifest_") + "appmanifest_".Length); //removes the path and additional text
                        appID = appID.Remove(appID.Length - 4); //removes .acf file extension.  4 is the length of ".acf"
                        installedGames.Add(new SteamGame(appID));
                    }
                }
            }                  

            return installedGames;

        }

        public void GetGamesListFromString()
        {
            Games = new List<SteamGame>();
            string temp = GamesString;

            for(int i = 0; i < temp.Length && i != -1; i = temp.IndexOf("<message>"))
            {
                string message = Utilities.ParseXML(temp, "message");
                string appID = Utilities.ParseXML(message, "appid");
                string playtime = Utilities.ParseXML(message, "playtime_forever");

                int playtimeInt;
                try
                {
                    playtimeInt = Int32.Parse(playtime);
                }
                catch(Exception ex) // :(
                {
                    Console.WriteLine(ex.Message);
                    BadProfile = true;
                    break;
                }                

                Games.Add(new SteamGame(appID, playtimeInt));                

                temp = temp.Substring(temp.IndexOf("</message>") + 1);
            }
        }
    }
}
