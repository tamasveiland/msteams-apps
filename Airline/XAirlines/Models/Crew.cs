using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Airlines.XAirlines.Models
{
    public class Crew
    {
        public string empID { get; set; }
        public Demographics personalDetails { get; set; }
        public string dept { get; set; }
        public List<Plan> plan { get; set; }
    }
    public class Demographics
    {
        public string name { get; set; }
        public string address { get; set; }
        public string contact { get; set; }
        public string emergencyContactName { get; set; }
        public string emergencyContactNumber { get; set; }
        public int workExperience { get; set; }
        public bool compliant { get; set; }
        public string licenceNumber { get; set; }
        public string issueDate { get; set; }
        public string expiryDate { get; set; }
        public int licencePoints { get; set; }
    }
    public class Plan
    {
        public string month { get; set; }
        public int weekNumber { get; set; }
        public string day { get; set; }
        public DateTime date { get; set; }
        public bool vacationPlan { get; set; }
        public DateTime vacationDate { get; set; }
        public string vacationReason { get; set; }
        public bool isDayOff { get; set; }
        public bool halt { get; set; }
        public DateTime lastUpdated { get; set; }
        public FlightDetails flightDetails { get; set; }
    }
    public class Days
    {
        public string day { get; set; }
        public string date { get; set; }
        public bool vacationPlan { get; set; }
        public string vacationDate { get; set; }
        public string vacationReason { get; set; }
        public bool isDayOff { get; set; }
        public bool halt { get; set; }
        public string lastUpdated { get; set; }
        public FlightDetails flightDetails { get; set; }
    }
    public class FlightDetails
    {
        public string code { get; set; }
        public DateTime flightStartDate { get; set; }
        public string source { get; set; }
        public string sourceCode { get; set; }
        public string sourceFlightCode { get; set; }
        public string sourceCurrencyCode { get; set; }
        public string flightDepartueTime { get; set; }
        public string destination { get; set; }
        public string destinationCode { get; set; }
        public string destinationFlightCode { get; set; }
        public string destinationCurrencyCode { get; set; }
        public DateTime flightEndDate { get; set; }
        public string flightArrivalTime { get; set; }
        public string layOVer { get; set; }
        public string travelDuraion { get; set; }
        public string gateNumber { get; set; }
        public string blockhours { get; set; }
        public string awayfromBase { get; set; }
        public string gateOpensAt { get; set; }
        public string acType { get; set; }
        public string tailNo { get; set; }
    }
}