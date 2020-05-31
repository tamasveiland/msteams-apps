using Microsoft.Bot.Connector.Teams.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Manufacturing.InventoryInfoBot.Model;
using AdaptiveCards;

using Microsoft.Bot.Connector;

namespace Manufacturing.InventoryInfoBot.Helper
{
    public static class O365CardHelper
    {
        public static Attachment GetListofProducts(IEnumerable<Product> products)
        {
            var listCard = new ListCard();
            listCard.content = new Content();

            var list = new List<Item>();

            foreach (var product in products)
            {
                string IndustryName;
                if (product.IndustryCode == "air")
                {
                    IndustryName = "Airlines";
                }
                else if (product.IndustryCode == "ret")
                {
                    IndustryName = "Retail";
                }
                else 
                {
                    IndustryName = "Manufacturing";
                }
                

                var item = new Item();
                listCard.content.title = "The following items are available for " + IndustryName;
                item.icon = product.ProductImageUrl;
                item.type = "resultItem";
                item.id = product.PrdouctId.ToString();
                var quantitiy = 0;
                var locationNames = "";
                foreach (var loc in product.locationList)
                {
                    quantitiy += loc.Quantity;
                    if (!string.IsNullOrEmpty(locationNames))
                        locationNames += ", ";
                    locationNames += loc.Location;
                }
                item.title = "Item code: " + product.ItemCode.ToString() + "   |" + " Item Name: " + product.ProductName;
                item.subtitle = "Quantity: " + quantitiy + "   |" + " Locations: " + locationNames;


                item.tap = new Tap()
                {
                    type = "imBack",
                    title = "ProductId",
                    value = "Show details of product" + product.ProductName + " (" + product.PrdouctId + ")"
                };
                list.Add(item);


            }

            listCard.content.items = list.ToArray();

            Attachment attachment = new Attachment();
            attachment.ContentType = listCard.contentType;
            attachment.Content = listCard.content;
            return attachment;
        }

        public static Attachment GetAdativeCard(Product product, string actionid)
        {
            var LocationItems = new List<AdaptiveElement>();
            LocationItems.Add(new AdaptiveTextBlock() { Text = "Location", Weight = AdaptiveTextWeight.Bolder, Size = AdaptiveTextSize.Medium, Wrap = true });
            foreach (var loc in product.locationList)
            {
                LocationItems.Add(new AdaptiveTextBlock() {Text = loc.Location, Weight = AdaptiveTextWeight.Default, Size = AdaptiveTextSize.Medium });
                
            }

            var Totalquantity = new List<AdaptiveElement>();
            Totalquantity.Add(new AdaptiveTextBlock() { Text = "Total", Weight = AdaptiveTextWeight.Bolder, Size = AdaptiveTextSize.Medium, Wrap = true });
            foreach (var total in product.locationList)
            {
                Totalquantity.Add(new AdaptiveTextBlock() { Text = total.Quantity.ToString(), Weight = AdaptiveTextWeight.Default, Size = AdaptiveTextSize.Medium });
            }
            var TotalCommited = new List<AdaptiveElement>();
            TotalCommited.Add(new AdaptiveTextBlock() { Text = "Committed", Weight = AdaptiveTextWeight.Bolder, Size = AdaptiveTextSize.Medium, Wrap = true });
            foreach (var total in product.locationList)
            {
                TotalCommited.Add(new AdaptiveTextBlock() { Text = total.Committed.ToString(), Weight = AdaptiveTextWeight.Default, Size = AdaptiveTextSize.Medium });
            }
            var RemainingList = new List<AdaptiveElement>();
            RemainingList.Add(new AdaptiveTextBlock() { Text = "Available", Weight = AdaptiveTextWeight.Bolder, Size = AdaptiveTextSize.Medium, Wrap = true });
            foreach (var total in product.locationList)
            {
                int balance = total.Quantity - total.Committed;
                RemainingList.Add(new AdaptiveTextBlock() { Text = balance.ToString(), Weight = AdaptiveTextWeight.Default, Size = AdaptiveTextSize.Medium });
            }

            AdaptiveCard card = new AdaptiveCard();

            var card3 = new AdaptiveCard()
            {
                Body = new List<AdaptiveElement>()
                {
                    new AdaptiveContainer
                    {
                        Items=new List<AdaptiveElement>()
                        {
                             new AdaptiveColumnSet()
                    {
                        Columns=new List<AdaptiveColumn>()
                        {

                            new AdaptiveColumn()
                            {
                                Width=AdaptiveColumnWidth.Auto,
                                Items=new List<AdaptiveElement>()
                                {

                                    new AdaptiveImage(){Size=AdaptiveImageSize.Medium,Url=new System.Uri(product.ProductImageUrl), Style=AdaptiveImageStyle.Default},
                                }

                            },

                            new AdaptiveColumn()
                            {
                                Width=AdaptiveColumnWidth.Stretch,
                                Items=new List<AdaptiveElement>()
                                {
                                    new AdaptiveTextBlock(){Text=$"Item Name: **{product.ProductName}**",Weight=AdaptiveTextWeight.Bolder,Size=AdaptiveTextSize.Medium,Wrap=true},
                                    new AdaptiveTextBlock(){Text=$"Item Code: **{product.ItemCode}**",Weight=AdaptiveTextWeight.Bolder,Size=AdaptiveTextSize.Medium,Wrap=true},
                                }

                            }
                        }

                    },


                        }
                    },
                    new AdaptiveContainer
                    {
                        Items=new List<AdaptiveElement>()
                        {
                             new AdaptiveColumnSet()
                    {
                        Columns=new List<AdaptiveColumn>()
                        {

                            new AdaptiveColumn()
                            {
                                Width=AdaptiveColumnWidth.Stretch,
                                Items=LocationItems

                            },

                            new AdaptiveColumn()
                            {
                                Width=AdaptiveColumnWidth.Stretch,
                                Items=Totalquantity

                            },
                             new AdaptiveColumn()
                            {
                                Width=AdaptiveColumnWidth.Stretch,
                                Items=TotalCommited

                            },
                              new AdaptiveColumn()
                            {
                                Width=AdaptiveColumnWidth.Stretch,
                                Items=RemainingList

                            }
                        }

                    },
            


                        }
                    }
                },

                Actions = new List<AdaptiveAction>()
                {
                    new AdaptiveShowCardAction()
                    {
                        Title="Add Inventory",
                        Card=new AdaptiveCard()
                       {
                          Body=new List<AdaptiveElement>()
                          {
                              new AdaptiveTextInput(){Id="newItemCount", Placeholder="Enter item count",IsRequired=true},
                              new AdaptiveTextInput(){Id="Location", Placeholder="Enter Location", IsRequired=true}
                             
                          },
                          Actions=new List<AdaptiveAction>()
                          {
                              new AdaptiveSubmitAction()
                              {
                                  Title="Add Inventory",
                                  DataJson= @"{'ProductId':'"+product.PrdouctId+"', 'Type':'" + Constants.newInventoryCount +"','ActionId':'" + actionid+"'}"

                              }
                          }
                       }
                       
                    },
                     new AdaptiveShowCardAction()
                    {
                        Title="Block Inventory",
                        Card=new AdaptiveCard()
                       {
                          Body=new List<AdaptiveElement>()
                          {
                              new AdaptiveTextInput(){Id="newItemCount", Placeholder="Enter item count",IsRequired=true},
                              new AdaptiveTextInput(){Id="Location", Placeholder="Enter Location", IsRequired=true}
                          },
                          Actions=new List<AdaptiveAction>()
                          {
                              new AdaptiveSubmitAction()
                              {
                                  Title="Block Inventory",
                                  DataJson= @"{'ProductId':'"+product.PrdouctId+"', 'Type':'" + Constants.BlockInventory+"', 'ActionId':'" + actionid+"'}"

                              }
                          }
                       }

                    },
                     new AdaptiveShowCardAction()
                    {
                        Title="Retire Inventory",
                        Card=new AdaptiveCard()
                       {
                          Body=new List<AdaptiveElement>()
                          {
                              new AdaptiveTextInput(){Id="newItemCount", Placeholder="Enter item count",IsRequired=true},
                              new AdaptiveTextInput(){Id="Location", Placeholder="Enter Location", IsRequired=true}
                          },
                          Actions=new List<AdaptiveAction>()
                          {
                              new AdaptiveSubmitAction()
                              {
                                  Title="Retire Inventory",
                                  DataJson= @"{'ProductId':'"+product.PrdouctId+"', 'Type':'" + Constants.RetireInventory+"', 'ActionId':'" + actionid+"'}"

                              }
                          }
                       }

                    },
                     new AdaptiveSubmitAction()
                     {
                         Title=Constants.RequestNewStock,
                         DataJson= @"{'Type':'" + Constants.RequestNewStock+"'}"
                     }
                    
                },
            };
            return new Attachment()
            {
                ContentType = AdaptiveCard.ContentType,
                Content = card3
            };
        }
            public static O365ConnectorCard GetO365ConnectorCardResult(Product product)
        {
            int balanceItems = product.Quantity - product.Committed;
            var actionId = Guid.NewGuid().ToString();

            var section = new O365ConnectorCardSection
            {
                ActivityTitle = $"Item Name: **{product.ProductName}**",
                ActivitySubtitle = $"Item Code: **{product.ItemCode}**",
                ActivityImage = product.ProductImageUrl,
                Facts = new List<O365ConnectorCardFact>
                    {

                        new O365ConnectorCardFact("Location", product.Location),
                        new O365ConnectorCardFact("Total Inventory", product.Quantity.ToString()),
                        new O365ConnectorCardFact("Quantity committed",product.Committed.ToString()),
                        new O365ConnectorCardFact("Quantity available",balanceItems.ToString()),

                    }
            };
            var addInventory = new O365ConnectorCardActionCard(
                O365ConnectorCardActionCard.Type,
                "Add Inventory",
                "Text Input",
                new List<O365ConnectorCardInputBase>
                {
                    new O365ConnectorCardTextInput(
                        O365ConnectorCardTextInput.Type,
                        "itemCount",
                        true,
                        "Number of items to add",
                        null,
                        false,
                        null)

               },


            new List<O365ConnectorCardActionBase>
                  {
                   new O365ConnectorCardHttpPOST(
                        O365ConnectorCardHttpPOST.Type,
                        "Add Inventory",
                        Constants.newInventoryCount,
                        @"{'newItem':'{{itemCount.value}}', 'ProductId':'"+product.PrdouctId+"'}")
                 });

            var blockInventory = new O365ConnectorCardActionCard(
                O365ConnectorCardActionCard.Type,
                "Block Inventory",
                "Text Input1",
                new List<O365ConnectorCardInputBase>
                {
                    new O365ConnectorCardTextInput(
                        O365ConnectorCardTextInput.Type,
                        "itemCount1",
                        true,
                        "Number of items to blocked",
                        null,
                        false,
                        null)

               },


            new List<O365ConnectorCardActionBase>
                  {
                   new O365ConnectorCardHttpPOST(
                        O365ConnectorCardHttpPOST.Type,
                        "Block Inventory",
                        Constants.BlockInventory,
                        @"{'newItem':'{{itemCount1.value}}', 'ProductId':'"+product.PrdouctId+"'}")
                 });
            var retireInventory = new O365ConnectorCardActionCard(
               O365ConnectorCardActionCard.Type,
               "Retire Inventory",
               "Text Input2",
               new List<O365ConnectorCardInputBase>
               {
                    new O365ConnectorCardTextInput(
                        O365ConnectorCardTextInput.Type,
                        "itemCount2",
                        true,
                        "Number of items to Retire",
                        null,
                        false,
                        null)

              },


           new List<O365ConnectorCardActionBase>
                 {
                   new O365ConnectorCardHttpPOST(
                        O365ConnectorCardHttpPOST.Type,
                        "Retire Inventory",
                        Constants.RetireInventory,
                        @"{'newItem':'{{itemCount2.value}}', 'ProductId':'"+product.PrdouctId+"'}")
                });
            O365ConnectorCard card = new O365ConnectorCard()
            {
                Title = "Product Information",
                ThemeColor = "#E67A9E",
                Sections = new List<O365ConnectorCardSection> { section },
                PotentialAction = new List<O365ConnectorCardActionBase>
                {
                    addInventory,
                    blockInventory,
                    retireInventory,
                    new O365ConnectorCardHttpPOST(O365ConnectorCardHttpPOST.Type, "Request for stock", Constants.RequestNewStock, $"{{'Value':'{product.PrdouctId}'}}"),
                }


            };
            return card;
        }
    }





    public class O365BodyValue
    {
        public string Value { get; set; }
    }

    public class InventoryInputDetails
    {
        public string newItemCount { get; set; }

        public string ProductId { get; set; }

        public string Type { get; set; }

        public string Location { get; set; }

        public string ActionId { get; set; }
    }

}