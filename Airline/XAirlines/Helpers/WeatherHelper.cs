using System;
using System.IO;
using System.Net;
using System.Web.Script.Serialization;

namespace Airlines.XAirlines.Helpers
{
    public class WeatherHelper
    {
        public static WeatherInfo GetWeatherInfo(string des)
        {
            string url = string.Format("http://api.openweathermap.org/data/2.5/weather?q={0}&APPID=619590f1e4a82a6ed18ee9b109bb9c14", des);
            string backupDataLocation = System.Web.Hosting.HostingEnvironment.MapPath(@"~\TestData\WeatherBackupMockData\");
            if (!Directory.Exists(backupDataLocation))
                Directory.CreateDirectory(backupDataLocation);
            var fileName = backupDataLocation + des + ".json";
            using (WebClient client = new WebClient())
            {
                WeatherInfo weatherinfo = null;
                string json = null;
                try
                {
                    json = client.DownloadString(url);
                    weatherinfo = (new JavaScriptSerializer().Deserialize<WeatherInfo>(json));
                    if (weatherinfo.cod != 200)
                    {
                        if (File.Exists(fileName))
                        {
                            json = File.ReadAllText(fileName);
                            weatherinfo = (new JavaScriptSerializer().Deserialize<WeatherInfo>(json));
                        }
                        else return null;
                    }
                }
                catch (Exception)
                {
                    if (File.Exists(fileName))
                    {
                        json = File.ReadAllText(fileName);
                        weatherinfo = (new JavaScriptSerializer().Deserialize<WeatherInfo>(json));

                    }
                }
                File.WriteAllText(backupDataLocation + des + ".json", json);
                return weatherinfo;
            }
        }
    }
    public class WeatherInfo
    {
        public Coord coord { get; set; }
        public Weather[] weather { get; set; }
        public string _base { get; set; }
        public Main main { get; set; }
        public Wind wind { get; set; }
        public Clouds clouds { get; set; }
        public int dt { get; set; }
        public Sys sys { get; set; }
        public int id { get; set; }
        public string name { get; set; }
        public int cod { get; set; }
    }
    public class Coord
    {
        public float lon { get; set; }
        public float lat { get; set; }
    }
    public class Main
    {
        public float temp { get; set; }
        public float pressure { get; set; }
        public int humidity { get; set; }
        public float temp_min { get; set; }
        public float temp_max { get; set; }
        public float sea_level { get; set; }
        public float grnd_level { get; set; }
    }
    public class Wind
    {
        public float speed { get; set; }
        public float deg { get; set; }
    }
    public class Clouds
    {
        public int all { get; set; }
    }
    public class Sys
    {
        public float message { get; set; }
        public string country { get; set; }
        public int sunrise { get; set; }
        public int sunset { get; set; }
    }
    public class Weather
    {
        public int id { get; set; }
        public string main { get; set; }
        public string description { get; set; }
        public string icon { get; set; }
    }
}
