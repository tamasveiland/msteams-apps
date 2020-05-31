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
namespace CrossVertical.Announcement.Models
{
    public class ListCard
    {
        public Content content { get; set; }

        public string contentType { get; set; } = "application/vnd.microsoft.teams.card.list";
    }

    public class Content
    {
        public string title { get; set; }
        public Item[] items { get; set; }
        public Button[] buttons { get; set; }
    }

    public class Item
    {
        public string type { get; set; }
        public string title { get; set; }
        public string id { get; set; }
        public string subtitle { get; set; }
        public Tap tap { get; set; }
        public string icon { get; set; }
    }

    public class Tap
    {
        public string type { get; set; }
        public string title { get; set; }
        public string value { get; set; }
    }

    public class Button
    {
        public string type { get; set; }
        public string title { get; set; }
        public string value { get; set; }
    }
}