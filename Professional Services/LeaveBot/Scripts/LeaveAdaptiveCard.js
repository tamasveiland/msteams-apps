function getAdaptiveCard() {
    var card = {
        "type": "AdaptiveCard",
        "version": "1.0",
        "body": [
            {
                "type": "Container",
                "items": [
                    {
                        "type": "ColumnSet",
                        "columns": [
                            {
                                "type": "Column",
                                "width": "50",
                                "items": [
                                    {
                                        "type": "TextBlock",
                                        "size": "medium",
                                        "weight": "lighter",
                                        "text": "From",
                                        "wrap": true
                                    },
                                    {
                                        "type": "Input.Date",
                                        "id": "FromDate",
                                        "placeholder": "From Date"
                                    }
                                ]
                            },
                            {
                                "type": "Column",
                                "width": "50",
                                "items": [
                                    {
                                        "type": "TextBlock",
                                        "size": "medium",
                                        "weight": "lighter",
                                        "text": "Duration",
                                        "wrap": true
                                    },
                                    {
                                        "type": "Input.ChoiceSet",
                                        "id": "FromDuration",
                                        "value": "FullDay",
                                        "style": "compact",
                                        "isMultiSelect": false,
                                        "choices": [
                                            {
                                                "title": "FullDay",
                                                "value": "FullDay"
                                            },
                                            {
                                                "title": "HalfDay",
                                                "value": "HalfDay"
                                            }
                                        ]
                                    }
                                ]
                            }
                        ]
                    }
                ]
            },
            {
                "type": "Container",
                "items": [
                    {
                        "type": "ColumnSet",
                        "columns": [
                            {
                                "type": "Column",
                                "width": "50",
                                "items": [
                                    {
                                        "type": "TextBlock",
                                        "size": "medium",
                                        "weight": "lighter",
                                        "text": "To",
                                        "wrap": true
                                    },
                                    {
                                        "type": "Input.Date",
                                        "id": "ToDate",
                                        "placeholder": "To Date"
                                    }
                                ]
                            },
                            {
                                "type": "Column",
                                "width": "50",
                                "items": [
                                    {
                                        "type": "TextBlock",
                                        "size": "medium",
                                        "weight": "lighter",
                                        "text": "Duration",
                                        "wrap": true
                                    },
                                    {
                                        "type": "Input.ChoiceSet",
                                        "id": "ToDuration",
                                        "value": "FullDay",
                                        "style": "compact",
                                        "isMultiSelect": false,
                                        "choices": [
                                            {
                                                "title": "FullDay",
                                                "value": "FullDay"
                                            },
                                            {
                                                "title": "HalfDay",
                                                "value": "HalfDay"
                                            }
                                        ]
                                    }
                                ]
                            }
                        ]
                    }
                ]
            },
            {
                "type": "Container",
                "items": [
                    {
                        "type": "TextBlock",
                        "weight": "lighter",
                        "color": "accent",
                        "text": "Please specify a reason for your leave"
                    }
                ]
            }
        ],
        "actions": [
            {
                "type": "Action.ShowCard",
                "card": {
                    "type": "AdaptiveCard",
                    "version": "1.0",
                    "body": [
                        {
                            "type": "Container",
                            "items": [
                                {
                                    "type": "ColumnSet",
                                    "columns": [
                                        {
                                            "type": "Column",
                                            "width": "auto",
                                            "items": [
                                                {
                                                    "type": "Image",
                                                    "url": "https://leavebotdemo.azurewebsites.net/Resources/Vacation-01.png"
                                                }
                                            ],
                                            "spacing": "none",
                                            "separation": "none"
                                        },
                                        {
                                            "type": "Column",
                                            "items": [
                                                {
                                                    "type": "TextBlock",
                                                    "text": "Yay! have a great Vacation!"
                                                }
                                            ],
                                            "spacing": "small"
                                        }
                                    ]
                                }
                            ]
                        },
                        {
                            "type": "Input.ChoiceSet",
                            "id": "LeaveType",
                            "value": "",
                            "style": "compact",
                            "isMultiSelect": false,
                            "choices": [
                                {
                                    "title": "Paid Leave",
                                    "value": "PaidLeave"
                                },
                                {
                                    "title": "Optional Leave",
                                    "value": "OptionalLeave"
                                },
                                {
                                    "title": "Carried over from last year",
                                    "value": "CarriedLeave"
                                }
                            ]
                        },
                        {
                            "type": "Input.Text",
                            "id": "LeaveReason",
                            "placeholder": "Comments (Optional)",
                            "value": "",
                            "isMultiline": true,
                            "maxLength": 300
                        }
                    ],
                    "actions": [
                        {
                            "type": "Action.Submit",
                            "data": {
                                "Type": "ApplyForVacation",
                                "LeaveId": ""
                            },
                            "title": "Submit"
                        }
                    ]
                },
                "title": "Vacation"
            },
            {
                "type": "Action.ShowCard",
                "card": {
                    "type": "AdaptiveCard",
                    "version": "1.0",
                    "body": [
                        {
                            "type": "Container",
                            "items": [
                                {
                                    "type": "ColumnSet",
                                    "columns": [
                                        {
                                            "type": "Column",
                                            "width": "auto",
                                            "items": [
                                                {
                                                    "type": "Image",
                                                    "url": "https://leavebotdemo.azurewebsites.net/Resources/HeartIcon.png"
                                                }
                                            ],
                                            "spacing": "none",
                                            "separation": "none"
                                        },
                                        {
                                            "type": "Column",
                                            "items": [
                                                {
                                                    "type": "TextBlock",
                                                    "text": "Get well soon!"
                                                }
                                            ],
                                            "spacing": "small"
                                        }
                                    ]
                                }
                            ]
                        },
                        {
                            "type": "Input.ChoiceSet",
                            "id": "LeaveType",
                            "value": "",
                            "style": "compact",
                            "isMultiSelect": false,
                            "choices": [
                                {
                                    "title": "Sick Leave",
                                    "value": "SickLeave"
                                }
                            ]
                        },
                        {
                            "type": "Input.Text",
                            "id": "LeaveReason",
                            "placeholder": "Comments (Optional)",
                            "value": "",
                            "isMultiline": true,
                            "maxLength": 300
                        }
                    ],
                    "actions": [
                        {
                            "type": "Action.Submit",
                            "data": {
                                "Type": "ApplyForSickLeave",
                                "LeaveId": ""
                            },
                            "title": "Submit"
                        }
                    ]
                },
                "title": "Sickness"
            },
            {
                "type": "Action.ShowCard",
                "card": {
                    "type": "AdaptiveCard",
                    "version": "1.0",
                    "body": [
                        {
                            "type": "Container",
                            "items": [
                                {
                                    "type": "ColumnSet",
                                    "columns": [
                                        {
                                            "type": "Column",
                                            "width": "auto",
                                            "items": [
                                                {
                                                    "type": "Image",
                                                    "url": "https://leavebotdemo.azurewebsites.net/Resources/Like.png"
                                                }
                                            ],
                                            "spacing": "none",
                                            "separation": "none"
                                        },
                                        {
                                            "type": "Column",
                                            "items": [
                                                {
                                                    "type": "TextBlock",
                                                    "text": "Go ahead"
                                                }
                                            ],
                                            "spacing": "small"
                                        }
                                    ]
                                }
                            ]
                        },
                        {
                            "type": "Input.ChoiceSet",
                            "id": "LeaveType",
                            "value": "",
                            "style": "compact",
                            "isMultiSelect": false,
                            "choices": [
                                {
                                    "title": "Paid Leave",
                                    "value": "PaidLeave"
                                },
                                {
                                    "title": "Optional Leave",
                                    "value": "OptionalLeave"
                                },
                                {
                                    "title": "Carried over from last year",
                                    "value": "CarriedLeave"
                                }
                            ]
                        },
                        {
                            "type": "Input.Text",
                            "id": "LeaveReason",
                            "placeholder": "Comments (Optional)",
                            "value": "",
                            "isMultiline": true,
                            "maxLength": 300
                        }
                    ],
                    "actions": [
                        {
                            "type": "Action.Submit",
                            "data": {
                                "Type": "ApplyForPersonalLeave",
                                "LeaveId": ""
                            },
                            "title": "Submit"
                        }
                    ]
                },
                "title": "Personal"
            },
            {
                "type": "Action.ShowCard",
                "card": {
                    "type": "AdaptiveCard",
                    "version": "1.0",
                    "body": [
                        {
                            "type": "Input.Text",
                            "id": "LeaveReason",
                            "placeholder": "Comments (Optional)",
                            "value": "",
                            "isMultiline": true,
                            "maxLength": 300
                        },
                        {
                            "type": "Input.ChoiceSet",
                            "id": "LeaveType",
                            "value": "",
                            "style": "compact",
                            "isMultiSelect": false,
                            "choices": [
                                {
                                    "title": "Optional Leave",
                                    "value": "OptionalLeave"
                                },
                                {
                                    "title": "Maternity Leave",
                                    "value": "MaternityLeave"
                                },
                                {
                                    "title": "Paternity Leave",
                                    "value": "PaternityLeave"
                                },
                                {
                                    "title": "Caregiver Leave",
                                    "value": "Caregiver"
                                }
                            ]
                        }
                    ],
                    "actions": [
                        {
                            "type": "Action.Submit",
                            "data": {
                                "Type": "ApplyForOtherLeave",
                                "LeaveId": ""
                            },
                            "title": "Submit"
                        }
                    ]
                },
                "title": "Other"
            }
        ]
    };
    return card;
};