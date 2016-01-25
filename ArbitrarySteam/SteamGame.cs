using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArbitrarySteam
{
    class SteamGame
    {
        public string Name { get; set; }
        public string AppID { get; set; }
        public double MinutesPlayed { get; set; } 

        public SteamGame(string appID)
        {
            AppID = appID;
        }

        public SteamGame(string appID, double hoursPlayed)
        {
            AppID = appID;
            MinutesPlayed = hoursPlayed;
        }

        public override bool Equals(object obj)
        {
            if (obj is SteamGame == false)
            {
                return false;
            }
                
            return Equals((SteamGame)obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public bool Equals(SteamGame other)
        {
            if(other == null)
            {
                return false;
            }

            return this.AppID == other.AppID;
        }
    }
}
