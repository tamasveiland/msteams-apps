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
using AdaptiveCards;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace CrossVertical.Announcement.Models
{
    /// <summary>
    /// POCO for celebration.
    /// </summary>
    public class Celebration : AnnouncementBase
    {
        public string Title { get; set; }
        public string CelebrationBannerImage { get; set; }
        public string Author { get; set; }
        public string Body { get; set; }

        public bool IsAcknowledgementRequested { get; set; }
        public MessageSensitivity Sensitivity { get; set; }
        public bool IsContactAllowed { get; set; }
        public override string Type { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public override AdaptiveCard GetCreateNewCard(List<Group> groups, List<Team> teams, bool isEditCard)
        {
            throw new NotImplementedException();
        }

        public override AdaptiveCard GetPreviewCard()
        {
            throw new NotImplementedException();
        }
    }
}