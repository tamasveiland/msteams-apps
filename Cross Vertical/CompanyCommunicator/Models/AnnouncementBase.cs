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
using System.Threading.Tasks;

namespace CrossVertical.Announcement.Models
{
    public abstract class AnnouncementBase : DatabaseItem
    {
        [JsonProperty("type")]
        public override string Type { get; set; } = nameof(AnnouncementBase);

        [JsonProperty("tenantId")]
        public string TenantId { get; set; }

        [JsonProperty("ownerId")]
        public string OwnerId { get; set; }

        public DateTime CreatedTime { get; set; }

        public RecipientInfo Recipients { get; set; } = new RecipientInfo();

        [JsonProperty("schedule")]
        public Schedule Schedule { get; set; }

        [JsonProperty("status")]
        public Status Status { get; set; }

        bool IsAcknowledgementRequested { get; set; }
        bool IsContactAllowed { get; set; }
        MessageSensitivity Sensitivity { get; set; }

        public abstract AdaptiveCard GetCreateNewCard(List<Group> groups, List<Team> teams, bool isEditCard);
        public abstract AdaptiveCard GetPreviewCard();
    }
}