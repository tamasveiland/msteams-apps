using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Manufacturing.InventoryInfoBot.Model
{
    

    public class Industry
    {
        public string IndustryName { get; set; }

        public string IndsutryCode { get; set; }
    }

    public class Product
    {
        [JsonProperty(PropertyName = "productId")]
        public int PrdouctId { get; set; }
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }
        [JsonProperty(PropertyName = "productName")]
        public string ProductName { get; set; }
        [JsonProperty(PropertyName = "quantity")]
        public int Quantity { get; set; }
        [JsonProperty(PropertyName = "isActive")]
        public bool IsActive { get; set;}
        [JsonProperty(PropertyName = "location")]
        public string Location { get; set; }
        [JsonProperty(PropertyName = "industryCode")]
        public string IndustryCode { get; set; }

        [JsonProperty(PropertyName = "productImage")]
        public string ProductImageUrl { get; set; }
        [JsonProperty(PropertyName = "committed")]
        public int Committed { get; set; }
        [JsonProperty(PropertyName = "itemCode")]
        public string ItemCode { get; set; }
        public List<Locationbased> locationList { get; set; }
    }

    public class Locationbased
    {
        public string Location { get; set; }

        public int Quantity { get; set; }

        public int Committed { get; set; }
    }
}