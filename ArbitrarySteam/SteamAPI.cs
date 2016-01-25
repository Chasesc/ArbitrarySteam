using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ArbitrarySteam
{
    class SteamAPI
    {
        public static string APIKey { get; set; } //Get this at http://steamcommunity.com/dev/apikey
        public static bool BadSteamKey { get; set; }

        public SteamUser User { get; set; }        

        public SteamAPI(string profileURL, bool isCustomURL)
        {
            APIKey = Properties.Settings.Default.SteamAPIKey;
            Console.WriteLine("key: " + APIKey);
            if(!IsValidSteamAPIKey())
            {
                HandleBadSteamKey();
            }

            User = new SteamUser();
            User.URL = "http://www." + profileURL;

            string profileInfo;

            if(!BadSteamKey)
            {
                if (isCustomURL)
                {
                    User.Name = RemoveExcessURL(profileURL, isCustomURL);
                    User.SteamID = GetSteamIDFromVanityName(User.Name);

                    if (String.IsNullOrEmpty(User.SteamID)) //given an invalid vanity name
                    {
                        User.BadProfile = true;
                    }
                    else //no point in trying these requests if we know they will fail
                    {
                        profileInfo = RequestPlayerSummary(User.SteamID);
                        User.AvatarUrl = Utilities.ParseXML(profileInfo, "avatarmedium");
                    }
                }
                else
                {
                    User.SteamID = RemoveExcessURL(profileURL, isCustomURL);
                    profileInfo = RequestPlayerSummary(User.SteamID);
                    User.Name = Utilities.ParseXML(profileInfo, "personaname");

                    if (String.IsNullOrEmpty(User.Name)) //given an invalid steam id
                    {
                        User.BadProfile = true;
                    }
                    else
                    {
                        User.AvatarUrl = Utilities.ParseXML(profileInfo, "avatarmedium");
                    }
                }
            }            

            if(!User.BadProfile)
            {
                User.GamesString = RequestSteamGamesListString(User.SteamID);
                User.GetGamesListFromString();
            }

            //Console.WriteLine(String.Format("\"{0}\" \"{1}\" \n\"{2}\" \n\"{3}\" \n\"{4}\"", User.Name, User.SteamID, User.URL, User.AvatarUrl, User.GamesString)); 
            Console.WriteLine("Finished with SteamAPI constructor");
        }

   
        private void HandleBadSteamKey()
        {                
            string[] mbLines =
            {                   
                "You need to add a free Steam API Key.  If you press 'OK' we will open the webpage where you can get one.",
                "",
                "Once you have a key, click on the settings icon in the top right to enter the key.",
                "",
                "Don't worry, you only need to do this once!"
            };

            MessageBoxResult result = MessageBox.Show(String.Join(Environment.NewLine, mbLines), "Invalid Steam Key", MessageBoxButton.OKCancel, MessageBoxImage.Information);

            if(result == MessageBoxResult.OK)
            {
                Utilities.GoToURL("https://steamcommunity.com/dev/apikey");
            }         
        }

        /*
         * This tests your Steam API key by trying to perform an api call with it.
         * 
         * returns bool: true if the API key is valid.  False otherwise
        */
        private bool IsValidSteamAPIKey()
        {
            string response;

            try
            {
               using(WebClient Client = new WebClient())
               {
                   
                   response = Client.DownloadString(String.Format("http://api.steampowered.com/IPlayerService/IsPlayingSharedGame/v0001/?key={0}&steamid=76561197960287930&appid_playing=240&format=xml", APIKey));
               }
            }
            catch(WebException ex)
            {
                Console.WriteLine(ex.Message);                
                BadSteamKey = true;

                return false;
            }

            BadSteamKey = false;
            return true;
        }

        public static string GetAppNameFromId(string appID)
        {
            try
            {
                using (WebClient Client = new WebClient())
                {
                    string name = "", apiResponse = Client.DownloadString(String.Format("http://store.steampowered.com/api/appdetails/?appids={0}&filters=basic", appID));

                    //TODO: IMPLEMENT THIS ON OWN
                    //The next few lines are from this:
                    //https://github.com/jshackles/idle_master/blob/master/Source/IdleMaster/frmMain.cs#L52-56

                    if (System.Text.RegularExpressions.Regex.IsMatch(apiResponse, "\"game\",\"name\":\"(.+?)\""))
                    {
                        name = System.Text.RegularExpressions.Regex.Match(apiResponse, "\"game\",\"name\":\"(.+?)\"").Groups[1].Value;
                    }

                    name = System.Text.RegularExpressions.Regex.Unescape(name);

                    //End

                    //we were able to get the name, yet there isn't one.  this means the game is no longer supported. EX. an alpha or beta of a game
                    if (String.IsNullOrEmpty(name)) { return "App no longer supported"; }


                    return name;
                }
            }
            catch (Exception except) { Console.WriteLine(except.Message); }


            return String.Empty;
        }

        private string RequestSteamGamesListString(string steamID)
        {
            try
            {
                using (WebClient Client = new WebClient())
                {
                    return Client.DownloadString(String.Format("http://api.steampowered.com/IPlayerService/GetOwnedGames/v0001/?key={0}&steamid={1}&format=xml", APIKey, steamID));
                }
            }
            catch (Exception except) { Console.WriteLine(except.Message); }

            return String.Empty;
        }

        private string GetSteamIDFromVanityName(string vanityName)
        {
            string xml = RequestSteamID(vanityName);
            if(Utilities.ParseXML(xml, "success") == "1")
            {
                return Utilities.ParseXML(xml, "steamid");
            }

            return String.Empty;
        }

        private string RequestSteamID(string vanityName)
        {
            try
            {
                using (WebClient Client = new WebClient())
                {
                    return Client.DownloadString(String.Format("http://api.steampowered.com/ISteamUser/ResolveVanityURL/v0001/?key={0}&vanityurl={1}&format=xml", APIKey, vanityName));
                }
            }
            catch (Exception except) { Console.WriteLine(except.Message); }

            return String.Empty;
        }

        private string RequestPlayerSummary(string steamID)
        {
            try
            {
                using (WebClient Client = new WebClient())
                {
                    return Client.DownloadString(String.Format("http://api.steampowered.com/ISteamUser/GetPlayerSummaries/v0002/?key={0}&steamids={1}&format=xml", APIKey, steamID));
                }
            }
            catch (Exception except) { Console.WriteLine(except.Message); }

            return String.Empty;
        }


        private string RemoveExcessURL(string profileURL, bool isCustomURL)
        {
            int startIndex;

            if(isCustomURL) //profileURL is in format "steamcommunity.com/id/THEIRVANITYNAME"
            {
                startIndex = profileURL.IndexOf(".com/id/") + ".com/id/".Length;
            }
            else //profileURL is in format "steamcommunity.com/profiles/THEIRSTEAM64ID"
            {
                startIndex = profileURL.IndexOf(".com/profiles/") + ".com/profiles/".Length;
            }

            return profileURL.Substring(startIndex);
        }
    }
}
