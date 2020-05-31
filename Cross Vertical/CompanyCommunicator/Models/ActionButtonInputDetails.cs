// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license.
//
// MIT License
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED ""AS IS"", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE. 
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CrossVertical.Announcement.Models
{
    public class ActionDetails
    {
        public string ActionType { get; set; }
    }

    public class ModeratorActionDetails : ActionDetails
    {
        public string Moderators { get; set; }
    }

    public class AnnouncementActionDetails : ActionDetails
    {
        public string Id { get; set; }
    }

    public class ScheduleAnnouncementActionDetails : AnnouncementActionDetails
    {
        public string Date { get; set; }

        public string Time { get; set; }
    }

    public class AnnouncementAcknowledgeActionDetails : AnnouncementActionDetails
    {
        public string GroupId { get; set; }

        public string UserId { get; set; }
    }

    public class CreateNewAnnouncementData : AnnouncementActionDetails
    {
        public string Groups { get; set; }
        public string Channels { get; set; }
        public string Title { get; set; }
        public string SubTitle { get; set; }
        public string AuthorAlias { get; set; }
        public string Image { get; set; }
        public string Preview { get; set; }
        public string Body { get; set; }
        public string Acknowledge { get; set; }
        public string AllowContactIns { get; set; }
        public string MessageType { get; set; }
    }

}