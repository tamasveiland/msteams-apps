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
using AdaptiveCards.Rendering.Html;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CrossVertical.Announcement.Models
{
    public class AnnouncementDetails
    {
        public int LikeCount { get; set; }
        public int AckCount { get; set; }
        public string Title { get; set; }
        public int ViewPercent { get; set; }
        public string RecipientsGroups { get; set; }
        public string RecipientChannels { get; set; }
        public HtmlTag html { get; set; }
        public static List<ReplyList> Replies = new List<ReplyList>()
        {
            new ReplyList(){ImagePath="~/",Name="MJ Price",Text="Looks great",Time=DateTime.Now},
            new ReplyList(){ImagePath="~/",Name="Marie Beaudoin",Text="Wonderful",Time=DateTime.Now}
        };
    }
    public class ReplyList
    {
        public string ImagePath { get; set; }
        public string Name { get; set; }
        public string Text { get; set; }
        public DateTime Time { get; set; }
    }
}