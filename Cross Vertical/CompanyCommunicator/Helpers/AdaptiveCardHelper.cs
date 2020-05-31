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
using Microsoft.Bot.Connector;

namespace CrossVertical.Announcement.Helpers
{
    /// <summary>
    ///  Adaptive card extension classes.
    /// </summary>
    public static class AdaptiveCardExtensions
    {
        public static Attachment ToAttachment(this AdaptiveCard card)
        {
            return new Attachment
            {
                Content = card,
                ContentType = AdaptiveCard.ContentType
            };
        }
    }

    public static class TaskModuleUIConstants
    {
        public static UIConstants CreateNewAnnouncement { get; set; } =
            new UIConstants(1200, 600, "Create New Announcement", "adaptivecard", "Adaptive Card");
    }
    public class UIConstants
    {
        public UIConstants(int width, int height, string title, string id, string buttonTitle)
        {
            Width = width;
            Height = height;
            Title = title;
            Id = id;
            ButtonTitle = buttonTitle;
        }

        public int Height { get; set; }
        public int Width { get; set; }
        public string Title { get; set; }
        public string ButtonTitle { get; set; }
        public string Id { get; set; }
    }
}