using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace ProfessionalServices.LeaveBot.Models
{
    public class LeaveDetails
    {
        [JsonIgnore]
        public const string TYPE = "LeaveDetails";

        [JsonProperty(PropertyName = "type")]
        public string Type { get; set; } = TYPE;

        [JsonProperty(PropertyName = "id")]
        public string LeaveId { get; set; }

        [JsonProperty(PropertyName = "appliedByEmailId")]
        public string AppliedByEmailId { get; set; }

        [JsonProperty(PropertyName = "managerEmailId")]
        public string ManagerEmailId { get; set; }

        [JsonProperty(PropertyName = "startDate")]
        public LeaveDate StartDate { get; set; }

        [JsonProperty(PropertyName = "endDate")]
        public LeaveDate EndDate { get; set; }

        [JsonProperty(PropertyName = "leaveType")]
        public LeaveType LeaveType { get; set; }

        [JsonProperty(PropertyName = "employeeComment")]
        public string EmployeeComment { get; set; }

        [JsonProperty(PropertyName = "managerComment")]
        public string ManagerComment { get; set; }

        [JsonProperty(PropertyName = "status")]
        public LeaveStatus Status { get; set; }

        [JsonProperty(PropertyName = "leaveCategory")]
        public LeaveCategory LeaveCategory { get; set; }

        [JsonProperty(PropertyName = "updateMessageInfo")]
        public UpdateMessageInfo UpdateMessageInfo { get; set; } = new UpdateMessageInfo();

        [JsonProperty(PropertyName = "channelId")]
        public string ChannelId { get; set; }

        [JsonProperty(PropertyName = "conversationId")]
        public string ConversationId { get; set; }
    }

    public class UpdateMessageInfo
    {
        [JsonProperty(PropertyName = "employee")]
        public string Employee { get; set; }

        [JsonProperty(PropertyName = "manager")]
        public string Manager { get; set; }
    }

    public class LeaveDate
    {
        [JsonProperty(PropertyName = "date")]
        public DateTime Date { get; set; }

        [JsonProperty(PropertyName = "type")]
        public DayType Type { get; set; }
    }

    public enum DayType
    {
        FullDay,
        HalfDay
    }

    public enum LeaveCategory
    {
        Vacation,
        Sickness,
        Personal,
        Other
    }

    public enum LeaveType
    {
        PaidLeave,
        SickLeave,
        OptionalLeave,
        CarriedLeave,
        MaternityLeave,
        PaternityLeave,
        Caregiver,
    }

    public enum LeaveStatus
    {
        Pending,
        Approved,
        Rejected,
        Withdrawn
    }

    public class LeaveExtended : LeaveDetails
    {
        public int DaysDiff { get; set; }

        public string startDay { get; set; }
        public string EndDay { get; set; }
        public string StartDateval { get; set; }

        public string EndDateVal { get; set; }
        public List<LeaveExtended> leavesData { get; set; }

        public int mgrTotalleaves { get; set; }

        public string lastUsed { get; set; }

        public string BaseUri { get; set; }
    }

    public class ManagerDetails : LeaveDetails
    {
        public bool IsManager { get; set; }

        public int mgrDaysdiff { get; set; }
        public string mgrstartDay { get; set; }
        public string mgrEndDay { get; set; }
        public string mgrStartDateval { get; set; }

        public string nextholiday { get; set; }

        public string mgrEndDateVal { get; set; }

        public List<LeaveExtended> mgrLeaveData
        {
            get; set;
        }

        public string resourceEmailid { get; set; }

        public string ResourceName { get; set; }
    }
}