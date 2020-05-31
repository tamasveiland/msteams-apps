using AdaptiveCards.Rendering.Html;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Airlines.XAirlines.Models
{
    public class ActionDetails:WelcomeActionDetails
    {
        public string ActionType { get; set; }
    }
    public class AirlineActionDetails : ActionDetails
    {
        public string Id { get; set; }
    }
    public class WelcomeActionDetails
    {
        public string Text { get; set; }
    }
    public class CityActionDetails : ActionDetails
    {
        public string City { get; set; }
    }
    public class WeatherActionDetails : CityActionDetails
    {
        public DateTime Date { get; set; }
    }
    public class CurrencyActionDetails : CityActionDetails
    {
        public string SourceCurrencyCode { get; set; }
        public string DestinationCurrencyCode { get; set; }
    }
    public class Portal
    {
        public HtmlTag html { get; set; }
    }
}