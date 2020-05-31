﻿// Copyright (c) Microsoft. All rights reserved.
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
    public class RecipientDetails
    {
        public string MessageId { get; set; }
        public string Id { get; set; }
        public string FailureMessage { get; set; }
        public bool IsAcknoledged { get; set; }
        public int LikeCount { get; set; }
        
    }

    public class PersonalMessageRecipients
    {
        public RecipientDetails RecipientDetails { get; set; }
        public User UserDetails { get; set; }
        public string GroupId { get; set; }
    }

    public class GroupRecipient
    {
        public string GroupId { get; set; }
        public List<RecipientDetails> Users { get; set; } = new List<RecipientDetails>();
    }

    public class ChannelRecipient
    {
        public string TeamId { get; set; }
        public RecipientDetails Channel { get; set; }
        public List<string> LikedUsers { get; set; } = new List<string>();
        public List<string> Members { get; set; } = new List<string>();
    }

}