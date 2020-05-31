using Newtonsoft.Json;
using System.Linq;

namespace ProfessionalServices.LeaveBot.Models
{
    public class Employee
    {
        [JsonIgnore]
        public const string TYPE = "Employee";

        [JsonProperty(PropertyName = "type")]
        public string Type { get; set; } = TYPE;

        [JsonProperty(PropertyName = "id")]
        public string EmailId { get; set; }

        [JsonProperty(PropertyName = "azureADId")]
        public string AzureADId { get; set; }

        [JsonProperty(PropertyName = "userUniqueId")]
        public string UserUniqueId { get; set; }

        [JsonProperty(PropertyName = "tenantId")]
        public string TenantId { get; set; }

        [JsonProperty(PropertyName = "photoPath")]
        public string PhotoPath { get; set; }

        [JsonProperty(PropertyName = "ManagerEmailId")]
        public string ManagerEmailId { get; set; }

        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [JsonIgnore]
        public string DisplayName { get { return (Name ?? string.Empty).Split(' ').First(); } }

        [JsonIgnore]
        public int Totalleaves { get; set; }

        [JsonIgnore]
        public bool IsManager { get; set; }

        [JsonIgnore]
        public string ManagerName { get; set; }

        [JsonProperty(PropertyName = "demoManagerEmailId")]
        public string DemoManagerEmailId { get; set; }

        [JsonProperty(PropertyName = "leaveBalance")]
        public LeaveBalance LeaveBalance { get; set; }

        [JsonProperty(PropertyName = "jobTitle")]
        public string JobTitle { get; set; }
    }

    public class LeaveBalance
    {
        public int SickLeave { get; set; }
        public int OptionalLeave { get; set; }
        public int PaidLeave { get; set; }
        public int CarriedLeave { get; set; }
        public int MaternityLeave { get; set; }
        public int PaternityLeave { get; set; }
        public int Caregiver { get; set; }

        // Add if needed
    }

    //public abstract class DocumentModel
    //{
    //    [JsonProperty(PropertyName = "type")]
    //    public abstract string Type { get; set; }
    //}
}