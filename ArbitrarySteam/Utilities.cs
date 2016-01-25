using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArbitrarySteam
{
    class Utilities
    {
        public static Random rng = new Random();

        public static string ParseXML(string xml, string searchTerm)
        {
            if (!xml.Contains(searchTerm)) { return String.Empty; }  //Can't parse what isn't there

            string startSearchTerm = String.Format("<{0}>", searchTerm);
            string endSearchTerm = String.Format("</{0}>", searchTerm);

            int start = xml.IndexOf(startSearchTerm) + startSearchTerm.Length;
            int end = xml.IndexOf(endSearchTerm);

            if ((end - start) <= 0) { return String.Empty; }

            return xml.Substring(start, end - start);
        }

        public static void GoToURL(string url)
        {
            try
            {
                Process.Start(url);
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }   
               
    }
}
