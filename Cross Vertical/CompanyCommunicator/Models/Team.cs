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
using Newtonsoft.Json;
using System.Collections.Generic;

namespace CrossVertical.Announcement.Models
{
    public class Team : DatabaseItem
    {
        [JsonProperty("type")]
        public override string Type { get; set; } = nameof(Team);

        public string Name { get; set; }

        [JsonProperty("aadObjectId")]
        public string AadObjectId { get; set; }

        public List<Channel> Channels { get; set; } = new List<Channel>();

        public List<string> Members { get; set; } = new List<string>();

        public static string GetTeamId(string id)
        {
            if (string.IsNullOrEmpty(id))
                return null;
            if (!id.Contains("@"))
                return id;

            return id.Replace("19:", "").Replace("@thread.skype", "");
        }
    }
}