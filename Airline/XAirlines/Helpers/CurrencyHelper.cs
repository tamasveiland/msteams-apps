using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Web.Script.Serialization;


namespace Airlines.XAirlines.Helpers
{
    public class CurrencyHelper
    {
        public static CurrencyInfo GetCurrencyInfo()
        {
            string url = string.Format("http://www.apilayer.net/api/live?access_key=29d0ff0f89f41d3bdd19f6c25ea4b1c4");
            string backupDataLocation = System.Web.Hosting.HostingEnvironment.MapPath(@"~\TestData\CurrencyBackupMockData\");
            if (!Directory.Exists(backupDataLocation))
                Directory.CreateDirectory(backupDataLocation);
            string fileName = Path.Combine(backupDataLocation, "Currencybackup.json");
            CurrencyInfo curr = null;
            using (WebClient client = new WebClient())
            {
                string json = null;
                try
                {
                    json = client.DownloadString(url);
                    curr = (new JavaScriptSerializer().Deserialize<CurrencyInfo>(json));
                    if (curr.success != true)
                    {
                        if (File.Exists(fileName))
                        {
                            json = File.ReadAllText(fileName);
                            curr = (new JavaScriptSerializer().Deserialize<CurrencyInfo>(json));
                        }
                    }
                    else
                        File.WriteAllText(fileName, json);
                }
                catch (Exception)
                {
                    if (File.Exists(fileName))
                    {
                        json = File.ReadAllText(fileName);
                        curr = (new JavaScriptSerializer().Deserialize<CurrencyInfo>(json));
                    }
                }
                return curr;
            }
        }
    }
    public class CurrencyInfo
    {
        public bool success { get; set; }
        public string terms { get; set; }
        public string privacy { get; set; }
        public int timestamp { get; set; }
        public string source { get; set; }
        public Dictionary<string, double> quotes { get; set; }
    }
}