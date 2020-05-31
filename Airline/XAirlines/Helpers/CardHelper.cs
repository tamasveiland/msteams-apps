using AdaptiveCards;
using Airlines.XAirlines.Common;
using Airlines.XAirlines.Models;
using Microsoft.Bot.Connector;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static Airlines.XAirlines.Helpers.WeatherHelper;

namespace Airlines.XAirlines.Helpers
{
    public class CardHelper
    {
        public static async Task<Attachment> GetWeeklyRosterCard(string userName)
        {
            List<Plan> weekplan = await CabinCrewPlansHelper.WeeksPlan(userName);
            var listCard = new ListCard();
            listCard.content = new Content();
            listCard.content.title = "Here is your next week's roster";
            var list = new List<Item>();
            foreach (var i in weekplan)
            {
                var item = new Item();
                item.id = i.flightDetails.flightStartDate.Date.ToString();
                item.type = "resultItem";
                item.icon = i.vacationPlan == true ? ApplicationSettings.BaseUrl + "/Resources/vacationicon.png" : i.isDayOff == true ? ApplicationSettings.BaseUrl + "/Resources/homeicon.png" : ApplicationSettings.BaseUrl + "/Resources/flighticon.png";
                item.title = i.vacationPlan == true ? Convert.ToDateTime(i.vacationDate).ToString("ddd dd MMM") : i.isDayOff == true ? Convert.ToDateTime(i.flightDetails.flightStartDate).ToString("ddd dd MMM") : Convert.ToDateTime(i.flightDetails.flightStartDate).ToString("ddd dd MMM") + " - " + Convert.ToDateTime(i.flightDetails.flightEndDate).ToString("ddd dd MMM");
                item.subtitle = i.vacationPlan == true ? i.vacationReason : i.isDayOff == true ? "Day Off" : i.flightDetails.sourceCode + "-" + i.flightDetails.destinationCode;
                item.tap = i.vacationPlan == true ? null : i.isDayOff == true ? null : new Tap()
                {
                    type = ActionTypes.MessageBack,
                    title = "Id",
                    value = JsonConvert.SerializeObject(new AirlineActionDetails()
                    {
                        Id = item.id,
                        ActionType = Constants.ShowDetailedRoster
                    })
                };
                list.Add(item);
            }

            listCard.content.items = list.ToArray();
            Attachment attachment = new Attachment();
            attachment.ContentType = listCard.contentType;
            attachment.Content = listCard.content;
            return attachment;
        }
        public static Attachment GetMonthlyRosterCard()
        {
            var Card = new AdaptiveCard(new AdaptiveSchemaVersion("1.0"))
            {
                Body = new List<AdaptiveElement>()
                     {
                    new AdaptiveContainer()
                    {
                        Items=new List<AdaptiveElement>()
                        {
                            new AdaptiveTextBlock()
                            {
                                Text="please access the portal tab to view the monthly roster",
                                Wrap=true
                            }
                        }
                    }
                },
            };
            Card.Actions.Add(new AdaptiveOpenUrlAction()
            {
                Title = "View Crew Portal",
                Url = new System.Uri(Constants.PortalTabDeeplink)
            });

            return new Attachment()
            {
                ContentType = AdaptiveCard.ContentType,
                Content = Card
            };
        }
        public static Attachment GetWelcomeScreen(string userName)
        {
            var Card = new AdaptiveCard(new AdaptiveSchemaVersion("1.0"))
            {
                Body = new List<AdaptiveElement>()
                 {
                    new AdaptiveContainer()
                    {
                        Items=new List<AdaptiveElement>()
                        {
                             new AdaptiveTextBlock()
                            {
                                Text=$"Hey {userName}! Here is what I can do for you",
                                Size=AdaptiveTextSize.Large
                            },
                            new AdaptiveColumnSet()
                            {
                                Columns=new List<AdaptiveColumn>()
                                {

                                    new AdaptiveColumn()
                                    {
                                         Width=AdaptiveColumnWidth.Auto,
                                         Items=new List<AdaptiveElement>()
                                         {
                                             new AdaptiveTextBlock(){Text="View Weekly roster",Color=AdaptiveTextColor.Accent,Size=AdaptiveTextSize.Medium, Spacing=AdaptiveSpacing.None, HorizontalAlignment=AdaptiveHorizontalAlignment.Center}
                                         },
                                          SelectAction = new AdaptiveSubmitAction()
                                         {
                                             DataJson=@"{'ActionType':'" + Constants.NextWeekRoster+"'}", Title="Next Week roster"
                                         }
                                    }
                                }
                            },
                            new AdaptiveColumnSet()
                            {
                                Columns=new List<AdaptiveColumn>()
                                {
                                    new AdaptiveColumn()
                                    {
                                         Width=AdaptiveColumnWidth.Auto,
                                         Items=new List<AdaptiveElement>()
                                         {
                                             new AdaptiveTextBlock(){Text="View Monthly Roster",Color=AdaptiveTextColor.Accent,Size=AdaptiveTextSize.Medium,HorizontalAlignment=AdaptiveHorizontalAlignment.Center,Spacing=AdaptiveSpacing.None }
                                         },
                                          SelectAction = new AdaptiveSubmitAction()
                                         {
                                                DataJson=@"{'ActionType':'" + Constants.NextMonthRoster+"'}", Title="Next Week roster"
                                         }
                                    }
                                }
                            },
                        }
                    }
                }
            };

            return new Attachment()
            {
                ContentType = AdaptiveCard.ContentType,
                Content = Card
            };
        }
        public static Attachment GetDetailedRoster(Plan datePlan)
        {
            if (datePlan == null)
                return new Attachment()
                {
                    Content = GetAdaptiveCardWithMessage("Unable to fetch duty details for specified date."),
                    ContentType = AdaptiveCard.ContentType
                };
            var Card = new AdaptiveCard(new AdaptiveSchemaVersion("1.0"))
            {
                Body = new List<AdaptiveElement>()
                     {
                    new AdaptiveColumnSet()
                    {
                        Spacing=AdaptiveSpacing.Small,
                        Columns=new List<AdaptiveColumn>()
                        {
                            new AdaptiveColumn()
                            {
                                Items=new List<AdaptiveElement>()
                                {
                                    new AdaptiveTextBlock()
                                    {
                                        HorizontalAlignment=AdaptiveHorizontalAlignment.Left,
                                        Spacing=AdaptiveSpacing.Small,
                                        Separator=true,
                                        Weight=AdaptiveTextWeight.Bolder,
                                        Color=AdaptiveTextColor.Attention,
                                        Text=datePlan.flightDetails.code,
                                        MaxLines=1
                                    }
                                },
                            },
                            new AdaptiveColumn()
                            {
                                Items=new List<AdaptiveElement>()
                                {
                                    new AdaptiveTextBlock()
                                    {
                                        HorizontalAlignment=AdaptiveHorizontalAlignment.Right,
                                        Separator=true,
                                        Text="Updated 2 days ago",
                                    }
                                },
                            },
                        }
                    },
                    new AdaptiveColumnSet()
                    {
                        Separator=true,
                        Columns=new List<AdaptiveColumn>()
                        {
                            new AdaptiveColumn()
                            {
                               Items=new List<AdaptiveElement>()
                               {
                                   new AdaptiveContainer()
                                   {
                                       Items=new List<AdaptiveElement>()
                                       {
                                            new AdaptiveTextBlock()
                                            {
                                               Size=AdaptiveTextSize.Medium,
                                                Weight=AdaptiveTextWeight.Bolder,
                                               Text=Convert.ToDateTime(datePlan.flightDetails.flightStartDate).ToString("ddd dd MMM")
                                            },
                                            new AdaptiveTextBlock()
                                            {
                                               Size=AdaptiveTextSize.Small,
                                               Weight=AdaptiveTextWeight.Bolder,
                                               Text=datePlan.flightDetails.flightDepartueTime
                                            },
                                       }
                                   }
                               }
                            },
                            new AdaptiveColumn()
                            {
                                Items=new List<AdaptiveElement>()
                                {
                                          new AdaptiveTextBlock()
                                            {
                                               Size=AdaptiveTextSize.Medium,
                                               HorizontalAlignment=AdaptiveHorizontalAlignment.Right,
                                                Weight=AdaptiveTextWeight.Bolder,
                                               Text=Convert.ToDateTime(datePlan.flightDetails.flightEndDate).ToString("ddd dd MMM")
                                            },
                                            new AdaptiveTextBlock()
                                            {
                                                HorizontalAlignment=AdaptiveHorizontalAlignment.Right,
                                               Size=AdaptiveTextSize.Small,
                                               Weight=AdaptiveTextWeight.Bolder,
                                               Text=datePlan.flightDetails.flightArrivalTime
                                            },
                                }
                            }
                        }
                    },
                    new AdaptiveColumnSet()
                    {
                      Separator=true,
                      Columns=new List<AdaptiveColumn>()
                      {
                          new AdaptiveColumn()
                          {
                              Items=new List<AdaptiveElement>()
                              {
                                 new AdaptiveTextBlock()
                                 {
                                     HorizontalAlignment=AdaptiveHorizontalAlignment.Center,
                                     Size=AdaptiveTextSize.Medium,
                                     Weight=AdaptiveTextWeight.Bolder,
                                     Text=datePlan.flightDetails.travelDuraion
                                 }
                              }
                          }
                      }
                    },
                    new AdaptiveColumnSet()
                    {
                        Columns=new List<AdaptiveColumn>()

                        {
                            new AdaptiveColumn()
                            {
                                Items=new List<AdaptiveElement>()
                                {
                                    new AdaptiveContainer()
                                    {
                                        Items=new List<AdaptiveElement>()
                                        {
                                            new AdaptiveTextBlock()
                                            {
                                                Size=AdaptiveTextSize.Small,
                                                Weight=AdaptiveTextWeight.Lighter,
                                                Text=datePlan.flightDetails.sourceFlightCode+"-"+datePlan.flightDetails.source

                                            },
                                            new AdaptiveTextBlock()
                                            {
                                                Size=AdaptiveTextSize.ExtraLarge,
                                                Color=AdaptiveTextColor.Accent,
                                                Text=datePlan.flightDetails.sourceCode
                                            }
                                        }
                                    }
                                }
                            },
                            new AdaptiveColumn()
                            {
                                Items=new List<AdaptiveElement>()
                                {
                                    new AdaptiveTextBlock()
                                    {
                                        HorizontalAlignment=AdaptiveHorizontalAlignment.Right,
                                        Size=AdaptiveTextSize.Small,
                                        Weight=AdaptiveTextWeight.Lighter,
                                        Text=datePlan.flightDetails.destinationFlightCode+"-"+datePlan.flightDetails.destination
                                    },
                                    new AdaptiveTextBlock()
                                    {
                                        HorizontalAlignment=AdaptiveHorizontalAlignment.Right,
                                        Size=AdaptiveTextSize.ExtraLarge,
                                       Color=AdaptiveTextColor.Accent,
                                        Text=datePlan.flightDetails.destinationCode
                                    },
                                    new AdaptiveTextBlock()
                                    {
                                        HorizontalAlignment=AdaptiveHorizontalAlignment.Right,
                                        Size=AdaptiveTextSize.Medium,
                                        Weight=AdaptiveTextWeight.Bolder,
                                        Color=AdaptiveTextColor.Accent,
                                        Text=datePlan.flightDetails.layOVer
                                    },
                                }
                            }
                        }

                    },
                    new AdaptiveColumnSet()
                    {
                        Columns=new List<AdaptiveColumn>()
                        {
                            new AdaptiveColumn()
                            {
                                Items=new List<AdaptiveElement>()
                                {
                                    new AdaptiveTextBlock()
                                    {
                                        Text="E-Gate Open"
                                    },
                                    new AdaptiveTextBlock()
                                    {
                                        Text="Block hrs"
                                    },
                                    new AdaptiveTextBlock()
                                    {
                                        Text="Away from base"
                                    },
                                    new AdaptiveTextBlock()
                                    {
                                        Text="AC type"
                                    },
                                    new AdaptiveTextBlock()
                                    {
                                        Text="Tail No"
                                    },
                                },

                            },
                            new AdaptiveColumn()
                            {
                                Items=new List<AdaptiveElement>()
                                {
                                    new AdaptiveTextBlock()
                                    {
                                        HorizontalAlignment=AdaptiveHorizontalAlignment.Right,
                                        Weight=AdaptiveTextWeight.Bolder,
                                        Text=datePlan.flightDetails.gateOpensAt
                                    },
                                    new AdaptiveTextBlock()
                                    {
                                        HorizontalAlignment=AdaptiveHorizontalAlignment.Right,
                                        Weight=AdaptiveTextWeight.Bolder,
                                        Text=datePlan.flightDetails.blockhours
                                    },
                                    new AdaptiveTextBlock()
                                    {
                                        HorizontalAlignment=AdaptiveHorizontalAlignment.Right,
                                        Weight=AdaptiveTextWeight.Bolder,
                                        Text=datePlan.flightDetails.awayfromBase
                                    },
                                    new AdaptiveTextBlock()
                                    {
                                        HorizontalAlignment=AdaptiveHorizontalAlignment.Right,
                                        Weight=AdaptiveTextWeight.Bolder,
                                        Text=datePlan.flightDetails.acType
                                    },
                                    new AdaptiveTextBlock()
                                    {
                                        HorizontalAlignment=AdaptiveHorizontalAlignment.Right,
                                        Weight=AdaptiveTextWeight.Bolder,
                                        Text=datePlan.flightDetails.tailNo
                                    },
                                }
                            }
                        },
                    }
                },
                Actions = new List<AdaptiveAction>()
                {
                    new AdaptiveSubmitAction()
                    {
                        Title="Weather Report",
                        Data=new WeatherActionDetails(){
                            Date =datePlan.flightDetails.flightEndDate.Date,
                            City =datePlan.flightDetails.destination,
                            ActionType =Constants.WeatherCard
                        }
                    },
                    new AdaptiveSubmitAction()
                    {
                        Title="Currency Details",
                        Data=new CurrencyActionDetails(){
                            SourceCurrencyCode =datePlan.flightDetails.sourceCurrencyCode,
                            DestinationCurrencyCode =datePlan.flightDetails.destinationCurrencyCode,
                            City =datePlan.flightDetails.destination,
                            ActionType =Constants.CurrencyCard}
                    }
                }
            };

            return new Attachment()
            {
                ContentType = AdaptiveCard.ContentType,
                Content = Card
            };
        }
        public static Attachment GetUpdateScreen()
        {
            var Card = new AdaptiveCard(new AdaptiveSchemaVersion("1.0"))
            {
                Body = new List<AdaptiveElement>()
                {
                    new AdaptiveContainer()
                    {
                        Items=new List<AdaptiveElement>()
                        {
                             new AdaptiveTextBlock()
                                    {
                                      Size=AdaptiveTextSize.Medium,
                                      Weight=AdaptiveTextWeight.Bolder,
                                      Text="Your monthly roster"
                                    },
                            new AdaptiveColumnSet()
                            {
                                Columns=new List<AdaptiveColumn>()
                                {
                                    new AdaptiveColumn()
                                    {
                                         Width=AdaptiveColumnWidth.Auto,
                                         Items=new List<AdaptiveElement>()
                                         {
                                             new AdaptiveTextBlock(){Text="New roster for the month of "+DateTime.Now.ToString("MMMM")+" has been released.Please acknowledge to view your new roster",Wrap=true}
                                         },
                                    },
                                    new AdaptiveColumn()
                                    {
                                         Width=AdaptiveColumnWidth.Auto,
                                         Items=new List<AdaptiveElement>()
                                         {
                                             new AdaptiveImage(){Url=new Uri(ApplicationSettings.BaseUrl + "/Resources/clipboard.PNG"),Id="abc"}
                                         }
                                    },
                                }
                            }
                        }
                    }
                }
            };
            Card.Actions.Add(new AdaptiveSubmitAction()
            {
                Title = "Acknowledge and view portal",
                Data = new ActionDetails() { ActionType = Constants.NextWeekRoster }
            });
            return new Attachment()
            {
                ContentType = AdaptiveCard.ContentType,
                Content = Card
            };
        }
        public static async Task<Attachment> GetWeatherCard(WeatherInfo wInfo, DateTime ArrivalDate)
        {
            var Card = new AdaptiveCard(new AdaptiveSchemaVersion("1.0"))
            {
                Body = new List<AdaptiveElement>()
                {
                    new AdaptiveContainer()
                    {
                        Items=new List<AdaptiveElement>()
                        {
                             new AdaptiveTextBlock()
                                    {
                                      Size=AdaptiveTextSize.Medium,
                                      Weight=AdaptiveTextWeight.Bolder,
                                      Text=string.IsNullOrEmpty(wInfo.name)?"Sorry we are not able to fetch the weather right now":"Here is the weather report for "+wInfo.name.ToUpper()
                                    },
                            new AdaptiveColumnSet()
                            {
                                Columns=new List<AdaptiveColumn>()
                                {
                                    new AdaptiveColumn()
                                    {
                                     Items=new List<AdaptiveElement>()
                                         {
                                             new AdaptiveTextBlock(){Text= "Date of Arrival",HorizontalAlignment=AdaptiveHorizontalAlignment.Left},
                                             new AdaptiveTextBlock(){Text=ArrivalDate.ToString("ddd,dd MMM"),Weight=AdaptiveTextWeight.Bolder}
                                         },
                                    },
                                    new AdaptiveColumn()
                                    {
                                        Items=new List<AdaptiveElement>()
                                        {
                                            new AdaptiveImage()
                                            {
                                                HorizontalAlignment=AdaptiveHorizontalAlignment.Right,
                                                Url=wInfo.weather.FirstOrDefault().description.Contains("rain")?new Uri(ApplicationSettings.BaseUrl+"/Resources/Rainwithcloud.png"):wInfo?.main.temp_min-273>25?new Uri(ApplicationSettings.BaseUrl+"/Resources/Sun-vector.png"):new Uri(ApplicationSettings.BaseUrl+"/Resources/Sunny-cloudy-weather.png"),
                                            }
                                        }
                                    }
                                }
                            },
                            new AdaptiveColumnSet()
                            {
                                Columns=new List<AdaptiveColumn>()
                                {
                                    new AdaptiveColumn()
                                    {
                                        Items=new List<AdaptiveElement>()
                                        {
                                            new AdaptiveImage()
                                            {
                                                Url=wInfo.weather[0].description.Contains("rain")?new Uri(ApplicationSettings.BaseUrl+"/Resources/Rainy.png"):wInfo.main.temp_min-273>25?new Uri(ApplicationSettings.BaseUrl+"/Resources/Sun.png"):new Uri(ApplicationSettings.BaseUrl+"/Resources/Sunny and cloudy.png")
                                            }
                                        }
                                    },
                                    new AdaptiveColumn()
                                    {
                                        Items=new List<AdaptiveElement>()
                                        {
                                            new AdaptiveTextBlock()
                                            {
                                                Text=wInfo.weather[0].description,
                                            }
                                        }
                                    }
                                }
                            },
                           new AdaptiveColumnSet()
                            {
                                Spacing=AdaptiveSpacing.Small,
                                Columns=new List<AdaptiveColumn>()
                                {
                                    new AdaptiveColumn()
                                    {
                                        Items=new List<AdaptiveElement>()
                                        {
                                            new AdaptiveTextBlock()
                                            {
                                                HorizontalAlignment=AdaptiveHorizontalAlignment.Left,
                                                Spacing=AdaptiveSpacing.Small,
                                                Separator=true,
                                                Weight=AdaptiveTextWeight.Lighter,
                                                Text="Minimum",
                                                MaxLines=1
                                            }
                                        }
                                    },
                                    new AdaptiveColumn()
                                    {
                                        Items=new List<AdaptiveElement>()
                                        {
                                            new AdaptiveTextBlock()
                                            {
                                                HorizontalAlignment=AdaptiveHorizontalAlignment.Right,
                                                Spacing=AdaptiveSpacing.Small,
                                                Separator=true,
                                                Weight=AdaptiveTextWeight.Lighter,
                                                Text="Maximum",
                                            }
                                        }
                                    },
                                }
                            },
                            new AdaptiveColumnSet()
                            {
                                Separator=true,
                                Columns=new List<AdaptiveColumn>()
                                {
                                    new AdaptiveColumn()
                                    {
                                        Items=new List<AdaptiveElement>()
                                        {
                                            new AdaptiveContainer()
                                            {
                                                Items=new List<AdaptiveElement>()
                                                {
                                                    new AdaptiveTextBlock()
                                                    {
                                                        Size=AdaptiveTextSize.Medium,
                                                        Weight=AdaptiveTextWeight.Bolder,
                                                         Text=Math.Round(wInfo.main.temp_min-273).ToString()
                                                    }
                                                }
                                            }
                                        }
                                    },
                                    new AdaptiveColumn()
                                    {
                                        Items=new List<AdaptiveElement>()
                                        {
                                            new AdaptiveTextBlock()
                                            {
                                                HorizontalAlignment=AdaptiveHorizontalAlignment.Right,
                                                Size=AdaptiveTextSize.Medium,
                                                Weight=AdaptiveTextWeight.Bolder,
                                                Text=Math.Round(wInfo.main.temp_max-273).ToString()
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            };
            return new Attachment()
            {
                ContentType = AdaptiveCard.ContentType,
                Content = Card
            };
        }
        private static AdaptiveCard GetTabWeatherCard(WeatherInfo wInfo, DateTime ArrivalDate)
        {
            if (wInfo == null)
                return GetAdaptiveCardWithMessage("Unable to fetch weather information right now. Please try again.");
            var Card = new AdaptiveCard(new AdaptiveSchemaVersion("1.0"))
            {
                Body = new List<AdaptiveElement>()
                {
                    new AdaptiveContainer()
                    {
                        Items=new List<AdaptiveElement>()
                        {
                            new AdaptiveTextBlock()
                                    {
                                      Size=AdaptiveTextSize.Medium,
                                      Weight=AdaptiveTextWeight.Bolder,
                                      Text="Here is the weather report for "+wInfo.name.ToUpper()
                                    },
                            new AdaptiveColumnSet()
                            {
                                Columns=new List<AdaptiveColumn>()
                                {
                                    new AdaptiveColumn()
                                    {
                                        Items =new List<AdaptiveElement>()
                                         {
                                             new AdaptiveTextBlock(){Text= "Date of Arrival",HorizontalAlignment=AdaptiveHorizontalAlignment.Left},
                                             new AdaptiveTextBlock(){Text=ArrivalDate.ToShortDateString(),Weight=AdaptiveTextWeight.Bolder}
                                         },
                                    },
                                    new AdaptiveColumn()
                                    {
                                        Items=new List<AdaptiveElement>()
                                        {
                                            new AdaptiveImage()
                                            {
                                                HorizontalAlignment=AdaptiveHorizontalAlignment.Right,
                                                Url=wInfo.weather[0].description.Contains("rain")?new Uri(ApplicationSettings.BaseUrl+"/Resources/Rainwithcloud.png"):wInfo.main.temp_min-273>25?new Uri(ApplicationSettings.BaseUrl+"/Resources/Sun-vector.png"):new Uri(ApplicationSettings.BaseUrl+"/Resources/Sunny-cloudy-weather.png"),
                                            }
                                        }
                                    }
                                }
                            },
                           new AdaptiveColumnSet()
                            {
                                Spacing=AdaptiveSpacing.Small,
                                Columns=new List<AdaptiveColumn>()
                                {
                                    new AdaptiveColumn()
                                    {
                                        Items=new List<AdaptiveElement>()
                                        {
                                            new AdaptiveTextBlock()
                                            {
                                                HorizontalAlignment=AdaptiveHorizontalAlignment.Left,
                                                Spacing=AdaptiveSpacing.Small,
                                                Separator=true,
                                                Weight=AdaptiveTextWeight.Lighter,
                                                Text="Minimum",
                                                MaxLines=1
                                            }
                                        }
                                    },
                                    new AdaptiveColumn()
                                    {
                                        Items=new List<AdaptiveElement>()
                                        {
                                            new AdaptiveTextBlock()
                                            {
                                                HorizontalAlignment=AdaptiveHorizontalAlignment.Right,
                                                Spacing=AdaptiveSpacing.Small,
                                                Separator=true,
                                                Weight=AdaptiveTextWeight.Lighter,
                                                Text="Maximum",

                                            }
                                        }
                                    },
                                }
                            },
                            new AdaptiveColumnSet()
                            {
                                Separator=true,
                                Columns=new List<AdaptiveColumn>()
                                {
                                    new AdaptiveColumn()
                                    {
                                        Items=new List<AdaptiveElement>()
                                        {
                                            new AdaptiveContainer()
                                            {
                                                Items=new List<AdaptiveElement>()
                                                {
                                                    new AdaptiveTextBlock()
                                                    {
                                                        Size=AdaptiveTextSize.Medium,
                                                        Weight=AdaptiveTextWeight.Bolder,
                                                         Text=Math.Round(wInfo.main.temp_min-273).ToString()
                                                    }
                                                }
                                            }
                                        }
                                    },
                                    new AdaptiveColumn()
                                    {
                                        Items=new List<AdaptiveElement>()
                                        {
                                            new AdaptiveTextBlock()
                                            {
                                                HorizontalAlignment=AdaptiveHorizontalAlignment.Right,
                                                Size=AdaptiveTextSize.Medium,
                                                Weight=AdaptiveTextWeight.Bolder,
                                                Text=Math.Round(wInfo.main.temp_max-273).ToString()
                                            }
                                        }
                                    }
                                }
                            }

                        }
                    }
                }
            };
            return Card;
        }
        public static async Task<Attachment> GetCurrencyCard(CurrencyInfo cInfo, string desCity, string desCurrencyCode)
        {
            AdaptiveCard Card = GetTabCurrencyCard(cInfo, desCity, desCurrencyCode);
            return new Attachment()
            {
                ContentType = AdaptiveCard.ContentType,
                Content = Card
            };
        }

        private static AdaptiveCard GetTabCurrencyCard(CurrencyInfo cInfo, string desCity, string desCurrencyCode)
        {
            if (cInfo == null)
            {
                var message = "Unable to fetch currency information right now. Please try again.";
                return GetAdaptiveCardWithMessage(message);
            }
            string desCode = cInfo.source + desCurrencyCode;
            double desCurrency;
            string desCurrencyTwo = string.Empty;
            desCurrency = Math.Round((cInfo.quotes[desCode]), 2);
            desCurrencyTwo = Math.Round((desCurrency / 100), 2).ToString();
            var Card = new AdaptiveCard(new AdaptiveSchemaVersion("1.0"))
            {
                Body = new List<AdaptiveElement>()
                {
                    new AdaptiveContainer()
                    {
                        Items =new List<AdaptiveElement>()
                        {
                             new AdaptiveColumnSet()
                            {
                                Spacing=AdaptiveSpacing.Small,
                                Columns=new List<AdaptiveColumn>()
                                {
                                    new AdaptiveColumn()
                                    {
                                        Items=new List<AdaptiveElement>()
                                        {
                                            new AdaptiveTextBlock()
                                            {
                                                HorizontalAlignment=AdaptiveHorizontalAlignment.Left,
                                                Spacing=AdaptiveSpacing.Small,
                                                Separator=true,
                                                Weight=AdaptiveTextWeight.Bolder,
                                                Text="Here are the currency details for " + desCity,
                                            }
                                        }
                                    }
                                }
                            },
                            new AdaptiveColumnSet()
                            {
                                Separator=true,
                                Columns=new List<AdaptiveColumn>()
                                {
                                    new AdaptiveColumn()
                                    {
                                        Items=new List<AdaptiveElement>()
                                        {
                                            new AdaptiveContainer()
                                            {
                                                Items=new List<AdaptiveElement>()
                                                {
                                                    new AdaptiveTextBlock()
                                                    {
                                                        Size=AdaptiveTextSize.Medium,
                                                        Weight=AdaptiveTextWeight.Lighter,
                                                        Text="1 " + cInfo.source
                                                    },
                                                    new AdaptiveTextBlock()
                                                    {
                                                        Size=AdaptiveTextSize.Medium,
                                                        Weight=AdaptiveTextWeight.Lighter,
                                                        Text="1 " + desCurrencyCode
                                                    },
                                                }
                                            }
                                        }
                                    },
                                    new AdaptiveColumn()
                                    {
                                        Items=new List<AdaptiveElement>()
                                        {
                                            new AdaptiveTextBlock()
                                            {
                                                HorizontalAlignment=AdaptiveHorizontalAlignment.Right,
                                                Size=AdaptiveTextSize.Medium,
                                                Weight=AdaptiveTextWeight.Bolder,
                                                Text=desCurrency + " " + desCurrencyCode
                                            },
                                            new AdaptiveTextBlock()
                                            {
                                                HorizontalAlignment=AdaptiveHorizontalAlignment.Right,
                                                Size=AdaptiveTextSize.Medium,
                                                Weight=AdaptiveTextWeight.Bolder,
                                                Text=desCurrencyTwo + " " + cInfo.source
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            };
            return Card;
        }
        private static AdaptiveCard GetAdaptiveCardWithMessage(string message)
        {
            return new AdaptiveCard(new AdaptiveSchemaVersion("1.0"))
            {
                Body = new List<AdaptiveElement>()
                {
                    new AdaptiveContainer()
                    {
                        Items=new List<AdaptiveElement>()
                        {
                             new AdaptiveColumnSet()
                            {
                                Spacing=AdaptiveSpacing.Small,
                                Columns=new List<AdaptiveColumn>()
                                {
                                    new AdaptiveColumn()
                                    {
                                        Items=new List<AdaptiveElement>()
                                        {
                                            new AdaptiveTextBlock()
                                            {
                                                HorizontalAlignment=AdaptiveHorizontalAlignment.Left,
                                                Spacing=AdaptiveSpacing.Small,
                                                Separator=true,
                                                Weight=AdaptiveTextWeight.Bolder,
                                                Color = AdaptiveTextColor.Warning,
                                                Text=message,
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

            };
        }
        public static async Task<Attachment> GetMyDetailedCard(string code, string userEmailId)
        {
            Crew crew = await CabinCrewPlansHelper.ReadJson(userEmailId);
            var dayPlan = crew.plan.FirstOrDefault(c => c.date.Date == Convert.ToDateTime(code));
            if (dayPlan == null)
                return null;
            WeatherInfo weatherinfo = GetWeatherInfo(dayPlan.flightDetails.destination);
            CurrencyInfo currencyinfo = CurrencyHelper.GetCurrencyInfo();
            var Card = new AdaptiveCard(new AdaptiveSchemaVersion("1.0"))
            {
                Body = new List<AdaptiveElement>()
                     {
                    new AdaptiveColumnSet()
                    {
                        Spacing=AdaptiveSpacing.Small,
                        Columns=new List<AdaptiveColumn>()
                        {
                            new AdaptiveColumn()
                            {
                                Items=new List<AdaptiveElement>()
                                {
                                    new AdaptiveTextBlock()
                                    {
                                        HorizontalAlignment=AdaptiveHorizontalAlignment.Left,
                                        Spacing=AdaptiveSpacing.Small,
                                        Separator=true,
                                        Weight=AdaptiveTextWeight.Bolder,
                                        Color=AdaptiveTextColor.Attention,
                                        Text=dayPlan.flightDetails.code,
                                        MaxLines=1
                                    }
                                },
                            },
                            new AdaptiveColumn()
                            {
                                Items=new List<AdaptiveElement>()
                                {
                                    new AdaptiveTextBlock()
                                    {
                                        HorizontalAlignment=AdaptiveHorizontalAlignment.Right,
                                        Separator=true,
                                        Text="Updated 2 days ago",
                                    }
                                },
                            },
                        }
                    },
                    new AdaptiveColumnSet()
                    {
                        Separator=true,
                        Columns=new List<AdaptiveColumn>()
                        {
                            new AdaptiveColumn()
                            {
                               Items=new List<AdaptiveElement>()
                               {
                                   new AdaptiveContainer()
                                   {
                                       Items=new List<AdaptiveElement>()
                                       {
                                            new AdaptiveTextBlock()
                                            {
                                               Size=AdaptiveTextSize.Medium,
                                                Weight=AdaptiveTextWeight.Bolder,
                                               Text=dayPlan.flightDetails.flightStartDate.ToString("ddd dd MMM")
                                            },
                                            new AdaptiveTextBlock()
                                            {
                                               Size=AdaptiveTextSize.Small,
                                               Weight=AdaptiveTextWeight.Bolder,
                                               Text=dayPlan.flightDetails.flightDepartueTime
                                            },
                                       }
                                   }
                               }
                            },
                            new AdaptiveColumn()
                            {
                                Items=new List<AdaptiveElement>()
                                {
                                          new AdaptiveTextBlock()
                                            {
                                               Size=AdaptiveTextSize.Medium,
                                               HorizontalAlignment=AdaptiveHorizontalAlignment.Right,
                                                Weight=AdaptiveTextWeight.Bolder,
                                               Text=dayPlan.flightDetails.flightEndDate.ToString("ddd dd MMM")
                                            },
                                            new AdaptiveTextBlock()
                                            {
                                                HorizontalAlignment=AdaptiveHorizontalAlignment.Right,
                                               Size=AdaptiveTextSize.Small,
                                               Weight=AdaptiveTextWeight.Bolder,
                                               Text=dayPlan.flightDetails.flightArrivalTime
                                            },
                                }
                            }
                        }
                    },
                    new AdaptiveColumnSet()
                    {
                      Separator=true,
                      Columns=new List<AdaptiveColumn>()
                      {
                          new AdaptiveColumn()
                          {
                              Items=new List<AdaptiveElement>()
                              {
                                 new AdaptiveTextBlock()
                                 {
                                     HorizontalAlignment=AdaptiveHorizontalAlignment.Center,
                                     Size=AdaptiveTextSize.Medium,
                                     Weight=AdaptiveTextWeight.Bolder,
                                     Text=dayPlan.flightDetails.travelDuraion
                                 }
                              }
                          }
                      }
                    },
                    new AdaptiveColumnSet()
                    {
                        Columns=new List<AdaptiveColumn>()

                        {
                            new AdaptiveColumn()
                            {
                                Items=new List<AdaptiveElement>()
                                {
                                    new AdaptiveContainer()
                                    {
                                        Items=new List<AdaptiveElement>()
                                        {
                                            new AdaptiveTextBlock()
                                            {
                                                Size=AdaptiveTextSize.Small,
                                                Weight=AdaptiveTextWeight.Lighter,
                                                Text=dayPlan.flightDetails.sourceFlightCode+"-"+dayPlan.flightDetails.source

                                            },
                                            new AdaptiveTextBlock()
                                            {
                                                Size=AdaptiveTextSize.ExtraLarge,
                                                Color=AdaptiveTextColor.Accent,
                                                Text=dayPlan.flightDetails.sourceCode
                                            }
                                        }
                                    }
                                }
                            },
                            new AdaptiveColumn()
                            {
                                Items=new List<AdaptiveElement>()
                                {
                                    new AdaptiveTextBlock()
                                    {
                                        HorizontalAlignment=AdaptiveHorizontalAlignment.Right,
                                        Size=AdaptiveTextSize.Small,
                                        Weight=AdaptiveTextWeight.Lighter,
                                        Text=dayPlan.flightDetails.destinationFlightCode+"-"+dayPlan.flightDetails.destination

                                    },
                                    new AdaptiveTextBlock()
                                    {
                                        HorizontalAlignment=AdaptiveHorizontalAlignment.Right,
                                        Size=AdaptiveTextSize.ExtraLarge,
                                       Color=AdaptiveTextColor.Accent,
                                        Text=dayPlan.flightDetails.destinationCode

                                    },
                                    new AdaptiveTextBlock()
                                    {
                                        HorizontalAlignment=AdaptiveHorizontalAlignment.Right,
                                        Size=AdaptiveTextSize.Medium,
                                        Weight=AdaptiveTextWeight.Bolder,
                                        Color=AdaptiveTextColor.Accent,
                                        Text=dayPlan.flightDetails.layOVer

                                    },
                                }
                            }
                        }
                    },
                    new AdaptiveColumnSet()
                    {
                        Columns=new List<AdaptiveColumn>()
                        {
                            new AdaptiveColumn()
                            {
                                Items=new List<AdaptiveElement>()
                                {
                                    new AdaptiveTextBlock()
                                    {
                                        Text="E-Gate Open"
                                    },
                                    new AdaptiveTextBlock()
                                    {
                                        Text="Block hrs"
                                    },
                                    new AdaptiveTextBlock()
                                    {
                                        Text="Away from base"
                                    },
                                    new AdaptiveTextBlock()
                                    {
                                        Text="AC type"
                                    },
                                    new AdaptiveTextBlock()
                                    {
                                        Text="Tail No"
                                    },
                                },
                            },
                            new AdaptiveColumn()
                            {
                                Items=new List<AdaptiveElement>()
                                {
                                    new AdaptiveTextBlock()
                                    {
                                        HorizontalAlignment=AdaptiveHorizontalAlignment.Right,
                                        Weight=AdaptiveTextWeight.Bolder,
                                        Text=dayPlan.flightDetails.gateOpensAt
                                    },
                                    new AdaptiveTextBlock()
                                    {
                                        HorizontalAlignment=AdaptiveHorizontalAlignment.Right,
                                        Weight=AdaptiveTextWeight.Bolder,
                                        Text=dayPlan.flightDetails.blockhours
                                    },
                                    new AdaptiveTextBlock()
                                    {
                                        HorizontalAlignment=AdaptiveHorizontalAlignment.Right,
                                        Weight=AdaptiveTextWeight.Bolder,
                                        Text=dayPlan.flightDetails.awayfromBase
                                    },
                                    new AdaptiveTextBlock()
                                    {
                                        HorizontalAlignment=AdaptiveHorizontalAlignment.Right,
                                        Weight=AdaptiveTextWeight.Bolder,
                                        Text=dayPlan.flightDetails.acType
                                    },
                                    new AdaptiveTextBlock()
                                    {
                                        HorizontalAlignment=AdaptiveHorizontalAlignment.Right,
                                        Weight=AdaptiveTextWeight.Bolder,
                                        Text=dayPlan.flightDetails.tailNo
                                    },
                                }
                            }
                        },
                    }
                },
                Actions = new List<AdaptiveAction>()
                {
                    new AdaptiveShowCardAction()
                    {
                        Title="Weather Report",
                        Card= GetTabWeatherCard(weatherinfo,dayPlan.flightDetails.flightEndDate.Date)
                    },
                    new AdaptiveShowCardAction()
                    {
                        Title="Currency Details",
                        Card=GetTabCurrencyCard(currencyinfo,dayPlan.flightDetails.destination,dayPlan.flightDetails.destinationCurrencyCode)
                    }
                }
            };
            return new Attachment()
            {
                ContentType = AdaptiveCard.ContentType,
                Content = Card
            };
        }
    }
}