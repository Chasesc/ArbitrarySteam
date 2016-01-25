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
            //SteamLocation is ..\Steam\steam.exe, we want to just have ..\Steam so we can add \steamapps to the end
            string steamLocTemp = Properties.Settings.Default.SteamLocation.Remove(Properties.Settings.Default.SteamLocation.IndexOf("steam.exe"));
            string steamAppsFolder = String.Format("{0}\\steamapps", steamLocTemp);
            
            List<SteamGame> installedGames = new List<SteamGame>();
            List<string> filesInSteamapps;

            try
            {
                filesInSteamapps = System.IO.Directory.GetFiles(steamAppsFolder, "*.acf").ToList<string>();
            }
            catch
            {
                return null;
            }
            

            foreach(string s in filesInSteamapps)
            {
                if(s.Contains("appmanifest_"))
                {
                    string appID = s.Substring(s.IndexOf("appmanifest_") + "appmanifest_".Length); //removes the path and additional text
                    appID = appID.Remove(appID.Length - 4); //removes .acf file extension.  4 is the length of ".acf"
                    installedGames.Add(new SteamGame(appID));
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
