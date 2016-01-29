using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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

namespace ArbitrarySteam
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private System.Windows.Visibility logonVisBefore, gameSelectorVisBefore;

        private SteamAPI steam;
        private SteamGame currentGame, previousGame;

        public MainWindow()
        {
            InitializeComponent();
            SetInstalledGames();
        }
      


        private void TextBoxLink_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!radioCustomURL.IsChecked.HasValue || !radioSteamID.IsChecked.HasValue) //IsChecked is a Nullable<bool>.  We can't continue if either are null.
            {
                return; 
            }

            //TODO: CHANGE THIS METHOD
            
            if((bool)radioCustomURL.IsChecked)
            {
                if(tbLink.Text.Length < "steamcommunity.com/id/".Length)
                {
                    tbLink.Text = "steamcommunity.com/id/";
                    tbLink.SelectionStart = tbLink.Text.Length + 1;
                }
            }
            else if((bool)radioSteamID.IsChecked)
            {
                if(tbLink.Text.Length < "steamcommunity.com/profiles/".Length)
                {
                    tbLink.Text = "steamcommunity.com/profiles/";
                    tbLink.SelectionStart = tbLink.Text.Length + 1;
                }
            }
        }

        private void DisplayInfoOrError(string message, Int32 msTime, bool isError = false)
        {
            tbErrorOrInfo.Text = message;
            tbErrorOrInfo.Visibility = System.Windows.Visibility.Visible;

            if(isError)
            {
                tbErrorOrInfo.Foreground = Brushes.Red;
            }

            System.Timers.Timer timer = new System.Timers.Timer(msTime);

            timer.Start();

            timer.Elapsed += (sender, args) =>
            {
                Dispatcher.BeginInvoke(new System.Threading.ThreadStart(delegate
                {                
                    tbErrorOrInfo.Text = string.Empty;
                    tbErrorOrInfo.Visibility = System.Windows.Visibility.Collapsed;

                    if (isError)
                    {
                        tbErrorOrInfo.Foreground = Brushes.White;
                    }

                }), System.Windows.Threading.DispatcherPriority.Input);

                timer.Dispose();
            };            
        }

        private void ButtonContinue_Click(object sender, RoutedEventArgs e)
        {
            if (tbLink.Text == "steamcommunity.com/id/" || tbLink.Text == "steamcommunity.com/profiles/") //The user wants to continue, but hasn't entered anything in
            {                     
                DisplayInfoOrError("You need to enter something first!", 3000);

                return;
            }            

            if(!radioCustomURL.IsChecked.HasValue) //IsChecked is a Nullable<bool>.  We can't continue if it's null.
            {
                DisplayInfoOrError("radioCustomURL.IsChecked is null!", 4000, true);
                return;
            }
    


            steam = new SteamAPI(tbLink.Text, (bool)radioCustomURL.IsChecked);

           
            if(SteamAPI.BadSteamKey)
            {
                DisplayInfoOrError("Invalid Steam Key", 3000, true);
                return;
            }
            else if(steam.User.BadProfile) //we were unable to find the steam account associated with the given URL
            {              
                DisplayInfoOrError("That account doesn't exist, or we couldn't connect to Steam's servers!", 5000, true);              
                return;
            }
            else if(steam.User.Games.Count <= 0)
            {
                DisplayInfoOrError("That steam user doesn't have any games!", 4000, true);
            }

            labelUserName.Content = steam.User.Name;

            NewGame();            

            logon.Visibility = System.Windows.Visibility.Collapsed;
            gameSelector.Visibility = System.Windows.Visibility.Visible;           
        }

        


        private void ButtonBack_Click(object sender, RoutedEventArgs e)
        {
            if(settings.Visibility == System.Windows.Visibility.Visible)
            {
                SaveSettings();
            }

            gameSelector.Visibility = settings.Visibility = System.Windows.Visibility.Collapsed;

            logon.Visibility = System.Windows.Visibility.Visible;
        }

        private void ButtonSettings_Click(object sender, RoutedEventArgs e)
        {            
            if(logon.Visibility == System.Windows.Visibility.Visible || gameSelector.Visibility == System.Windows.Visibility.Visible)
            {              
                ShowSettings();
            }
            else
            {
                logon.Visibility = logonVisBefore;
                gameSelector.Visibility = gameSelectorVisBefore;

                SaveSettings();
                settings.Visibility = System.Windows.Visibility.Collapsed;
            }      
        }



        private void ButtonLaunchOrDownload_Click(object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrEmpty(currentGame.AppID)) //better safe than sorry
            {
                DisplayInfoOrError("No game is currently selected.  How did this happen?", 5000, true);
                return;
            }

            try
            {
                Process.Start(Properties.Settings.Default.SteamLocation, String.Format("-applaunch {0}", currentGame.AppID));
            }
            catch
            {
                DisplayInfoOrError(String.Format("Unable to start game!  Change your steam location in the settings!\nLocation: {0}", Properties.Settings.Default.SteamLocation), 4000, true);
            }
        }
            
        private void ButtonNewRandomGame_Click(object sender, RoutedEventArgs e)
        {
            NewGame();
        }


        private void NewGame()
        {
            int randomIndex = Properties.Settings.Default.InstalledOnly ?
                    Utilities.rng.Next(Properties.Settings.Default.InstalledGames.Count) :
                    Utilities.rng.Next(steam.User.Games.Count);

            //TODO: clean this up. it's pretty messy

            //If we are only selecting from the pool of installed games, our current game is a random object from the list of installed games.
            //At this point, we do not have the playtime of that game within this object, but we do have it in the list of all owned games.
            //we find the same object from the list of all owned games using .Find and set that object as the current game.  This way we have the playtime
            //if we are selecting from the pool of all games, our current game is a random index from the list of all games.     
            //Reasoning: O(n) on a list with an expected size of around ~100 is better than an API call to get the time played.
            currentGame = Properties.Settings.Default.InstalledOnly ?
                    steam.User.Games.Find(Properties.Settings.Default.InstalledGames[randomIndex].Equals) :
                    steam.User.Games[randomIndex];

            //.Find returned null which means the user has an installed game which they do not own, do not show them this game
            if (currentGame == null)
            {
                NewGame();
            }

            currentGame.Name = SteamAPI.GetAppNameFromId(currentGame.AppID);

            if (currentGame.Equals(previousGame) || currentGame.Name == "App no longer supported") //Some "games" are alphas or betas that no longer work
            {
                NewGame();
            }


            tbGameName.Text = currentGame.Name;
            labelGameTime.Content = String.Format("You have {0} hours in this game.", (currentGame.MinutesPlayed / 60.0f).ToString("0.0")); //Steam's API returns time played in mins.  This converts to hours

            gameSelector.Background = new ImageBrush(new BitmapImage(new Uri(String.Format("http://cdn.akamai.steamstatic.com/steam/apps/{0}/page_bg_generated_v6b.jpg", currentGame.AppID))));
            gameImage.Source = new BitmapImage(new Uri(String.Format("http://cdn.akamai.steamstatic.com/steam/apps/{0}/header_292x136.jpg", currentGame.AppID)));

            if (Properties.Settings.Default.InstalledOnly) //we know they have this game installed
            {
                SetButtonLaunchOrDownloadImage("resources/images/launch_64x64.png");
                buttonLaunchOrDownload.ToolTip = "Launch the game";
            }
            else //We need to find out if they have this game installed to show them the correct image.
            {
                if (Properties.Settings.Default.InstalledGames.Contains(currentGame))
                {
                    SetButtonLaunchOrDownloadImage("resources/images/launch_64x64.png");
                    buttonLaunchOrDownload.ToolTip = "Launch the game";
                }
                else
                {
                    SetButtonLaunchOrDownloadImage("resources/images/download_64x64.png");
                    buttonLaunchOrDownload.ToolTip = "Download the game";
                }
            }

            previousGame = currentGame;
        }

        private void SetButtonLaunchOrDownloadImage(string imageLocation)
        {
            buttonLaunchOrDownload.Content = null;

            Image image = new Image();
            image.Source = new System.Windows.Media.Imaging.BitmapImage(new Uri(BaseUriHelper.GetBaseUri(this), imageLocation));
            image.Width = 64;
            image.Height = 64;

            StackPanel stackPan = new StackPanel();
            stackPan.Orientation = Orientation.Horizontal;
            stackPan.Children.Add(image);

            buttonLaunchOrDownload.Content = stackPan;
        }

        private void RadioCustomURL_Click(object sender, RoutedEventArgs e)
        {
            tbLink.Text = "steamcommunity.com/id/";
        }

        private void RadioSteamID_Click(object sender, RoutedEventArgs e)
        {
            tbLink.Text = "steamcommunity.com/profiles/";
        }

        private void GameImage_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Utilities.GoToURL(String.Format("http://store.steampowered.com/app/{0}", currentGame.AppID));
        }

        //Settings methods that need access to the UI elements
        #region Settings Stuff

        private void SaveSettings()
        {
            Properties.Settings.Default.SteamAPIKey = settings_tbAPIKey.Text;

            if (settings_checkOnlyInstalled.IsChecked.HasValue) //IsChecked is a Nullable<bool>.  We can't save its value to a normal bool if it's null.
            {
                Properties.Settings.Default.InstalledOnly = (bool)settings_checkOnlyInstalled.IsChecked;
            }

            List<string> steamDirectoriesBefore = Properties.Settings.Default.SteamDirectories;

            if(settings_lbDirList.Items.Count != 0)
            {
                Properties.Settings.Default.SteamLocation = settings_lbDirList.Items[0].ToString();

                //We skip the first element because it is the location of steam.exe, not a steamapps directory
                Properties.Settings.Default.SteamDirectories = settings_lbDirList.Items.Cast<string>().Skip(1).ToList();

                if(steamDirectoriesBefore != Properties.Settings.Default.SteamDirectories)
                {
                    SetInstalledGames();
                }                
            }
            
           
            Properties.Settings.Default.Save();
        }

        private void ShowSettings()
        {
            logonVisBefore = logon.Visibility;
            gameSelectorVisBefore = gameSelector.Visibility;

            logon.Visibility = gameSelector.Visibility = System.Windows.Visibility.Collapsed;

            settings_tbAPIKey.Text = Properties.Settings.Default.SteamAPIKey;
            settings_checkOnlyInstalled.IsChecked = Properties.Settings.Default.InstalledOnly;

            if(Properties.Settings.Default.SteamDirectories != null)
            {
                settings_lbDirList.Items.Clear();
                settings_lbDirList.Items.Add(Properties.Settings.Default.SteamLocation);                
                foreach (string str in Properties.Settings.Default.SteamDirectories)
                {
                    settings_lbDirList.Items.Add(str);
                } 
            }
                      
           

            settings.Visibility = System.Windows.Visibility.Visible;
        }

        private void ButtonSettings_buttonAdd_Click(object sender, RoutedEventArgs e)
        {
            //TODO: Make this method open the file browser instead

            string location = settings_tbDir.Text.ToLower();
            bool invalidInput = String.IsNullOrEmpty(location);

            //If you copy a file or folder path using shift + right click -> copy as path it adds quotes around your path.  remove them
            location = location.Replace("\"", String.Empty); 

            if (settings_lbDirList.Items.Contains(location)) { return; }

            if(settings_lbDirList.Items.Count == 0) //The first location must go directly to steam.exe
            {
                if (!location.Contains("steam.exe") || !File.Exists(location)) { invalidInput = true; }
            }
            else
            {
                if (!location.Contains("steamapps") || !Directory.Exists(location)) { invalidInput = true; }
            }

            if(invalidInput)
            {
                MessageBox.Show("The first input must be the location of steam.exe.\nAny additional input should be the location(s) of the steamapps directory.",
                        "Input information", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            settings_lbDirList.Items.Add(location);
        }

        private void ButtonSettings_buttonSub_Click(object sender, RoutedEventArgs e)
        {
            if(settings_lbDirList.Items.Count != 0)
            {
                if(settings_lbDirList.SelectedItem != null)
                {
                    settings_lbDirList.Items.Remove(settings_lbDirList.SelectedItem); //remove current item selected if it exists
                }
                else
                {
                    settings_lbDirList.Items.RemoveAt(settings_lbDirList.Items.Count - 1); //remove the last item if no item is selected
                }                
            }
        }

        private void SetInstalledGames()
        {
            Properties.Settings.Default.InstalledGames = SteamUser.GetInstalledGamesList();
            if (Properties.Settings.Default.InstalledGames == null)
            {
                DisplayInfoOrError("SteamLocation set incorrectly!  Please change it in the settings!", 5000, true);
            }
        }

        #endregion

       

       





    }
}
