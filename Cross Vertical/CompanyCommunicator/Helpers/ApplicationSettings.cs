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
using System.Configuration;
namespace CrossVertical.Announcement.Helper
{
    public static class ApplicationSettings
    {
        public static string AppName { get; set; } = "Company Communicator";

        public static string AppFeature { get; set; } = "message";

        public static string BaseUrl { get; set; }

        public static string ConnectionName { get; set; }

        public static string AppId { get; set; }

        public static string AppSecret { get; set; }

        public static int NoOfParallelTasks { get; set; } = 10;

        public static string GraphApiEndpoint { get; set; } = "https://graph.microsoft.com/beta/";


        static ApplicationSettings()
        {
            ConnectionName = ConfigurationManager.AppSettings["ConnectionName"];
            BaseUrl = ConfigurationManager.AppSettings["BaseUri"];

            AppId = ConfigurationManager.AppSettings["MicrosoftAppId"];
            AppSecret = ConfigurationManager.AppSettings["MicrosoftAppPassword"];

            NoOfParallelTasks = int.Parse(ConfigurationManager.AppSettings["NoOfParallelTasks"]);

            GraphApiEndpoint = ConfigurationManager.AppSettings["GraphEndpoint"]; 
        }
    }
}