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
using Microsoft.Bot.Connector;
using Newtonsoft.Json;
using System.Runtime.Serialization;

namespace TaskModule
{

    /// <summary>
    /// Metadata for a Task Module.
    /// </summary>
    public partial class TaskModuleTaskInfo
    {
        /// <summary>
        /// Initializes a new instance of the TaskModuleTaskInfo class.
        /// </summary>
        public TaskModuleTaskInfo() { }

        /// <summary>
        /// Initializes a new instance of the TaskModuleTaskInfo class.
        /// </summary>
        /// <param name="title">Appears below the app name and to the right of
        /// the app icon.</param>
        /// <param name="height">This can be a number, representing the task
        /// module's height in pixels, or a string, one of: small, medium,
        /// large.</param>
        /// <param name="width">This can be a number, representing the task
        /// module's width in pixels, or a string, one of: small, medium,
        /// large.</param>
        /// <param name="url">The URL of what is loaded as an iframe inside
        /// the task module. One of url or card is required.</param>
        /// <param name="card">The JSON for the Adaptive card to appear in the
        /// task module.</param>
        /// <param name="fallbackUrl">If a client does not support the task
        /// module feature, this URL is opened in a browser tab.</param>
        /// <param name="completionBotId">If a client does not support the
        /// task module feature, this URL is opened in a browser tab.</param>
        public TaskModuleTaskInfo(string title = default(string), object height = default(object), object width = default(object), string url = default(string), Attachment card = default(Attachment), string fallbackUrl = default(string), string completionBotId = default(string))
        {
            Title = title;
            Height = height;
            Width = width;
            Url = url;
            Card = card;
            FallbackUrl = fallbackUrl;
            CompletionBotId = completionBotId;
        }

        /// <summary>
        /// Gets or sets appears below the app name and to the right of the
        /// app icon.
        /// </summary>
        [Newtonsoft.Json.JsonProperty(PropertyName = "title")]
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets this can be a number, representing the task module's
        /// height in pixels, or a string, one of: small, medium, large.
        /// </summary>
        [Newtonsoft.Json.JsonProperty(PropertyName = "height")]
        public object Height { get; set; }

        /// <summary>
        /// Gets or sets this can be a number, representing the task module's
        /// width in pixels, or a string, one of: small, medium, large.
        /// </summary>
        [Newtonsoft.Json.JsonProperty(PropertyName = "width")]
        public object Width { get; set; }

        /// <summary>
        /// Gets or sets the URL of what is loaded as an iframe inside the
        /// task module. One of url or card is required.
        /// </summary>
        [Newtonsoft.Json.JsonProperty(PropertyName = "url")]
        public string Url { get; set; }

        /// <summary>
        /// Gets or sets the JSON for the Adaptive card to appear in the task
        /// module.
        /// </summary>
        [Newtonsoft.Json.JsonProperty(PropertyName = "card")]
        public Attachment Card { get; set; }

        /// <summary>
        /// Gets or sets if a client does not support the task module feature,
        /// this URL is opened in a browser tab.
        /// </summary>
        [Newtonsoft.Json.JsonProperty(PropertyName = "fallbackUrl")]
        public string FallbackUrl { get; set; }

        /// <summary>
        /// Gets or sets if a client does not support the task module feature,
        /// this URL is opened in a browser tab.
        /// </summary>
        [Newtonsoft.Json.JsonProperty(PropertyName = "completionBotId")]
        public string CompletionBotId { get; set; }

    }

    /// <summary>
    /// Envelope for Task Module Response.
    /// </summary>
    public partial class TaskModuleResponseEnvelope
    {
        /// <summary>
        /// Initializes a new instance of the TaskModuleResponseEnvelope class.
        /// </summary>
        public TaskModuleResponseEnvelope() { }

        /// <summary>
        /// Initializes a new instance of the TaskModuleResponseEnvelope class.
        /// </summary>
        /// <param name="task">The JSON for the Adaptive card to appear in the
        /// task module.</param>
        public TaskModuleResponseEnvelope(TaskModuleResponseBase task = default(TaskModuleResponseBase))
        {
            Task = task;
        }

        /// <summary>
        /// Gets or sets the JSON for the Adaptive card to appear in the task
        /// module.
        /// </summary>
        [Newtonsoft.Json.JsonProperty(PropertyName = "task")]
        public TaskModuleResponseBase Task { get; set; }

    }


    /// <summary>
    /// Base class for Task Module responses
    /// </summary>
    public partial class TaskModuleResponseBase
    {
        /// <summary>
        /// Initializes a new instance of the TaskModuleResponseBase class.
        /// </summary>
        public TaskModuleResponseBase() { }

        /// <summary>
        /// Initializes a new instance of the TaskModuleResponseBase class.
        /// </summary>
        /// <param name="type">Choice of action options when responding to the
        /// task/submit message. Possible values include: 'message',
        /// 'continue'</param>
        public TaskModuleResponseBase(string type = default(string))
        {
            Type = type;
        }

        /// <summary>
        /// Gets or sets choice of action options when responding to the
        /// task/submit message. Possible values include: 'message',
        /// 'continue'
        /// </summary>
        [Newtonsoft.Json.JsonProperty(PropertyName = "type")]
        public string Type { get; set; }

    }

    /// <summary>
    /// Task Module response with message action.
    /// </summary>
    public partial class TaskModuleMessageResponse : TaskModuleResponseBase
    {
        /// <summary>
        /// Initializes a new instance of the TaskModuleMessageResponse class.
        /// </summary>
        public TaskModuleMessageResponse() { }

        /// <summary>
        /// Initializes a new instance of the TaskModuleMessageResponse class.
        /// </summary>
        /// <param name="type">Choice of action options when responding to the
        /// task/submit message. Possible values include: 'message',
        /// 'continue'</param>
        /// <param name="value">Teams will display the value of value in a
        /// popup message box.</param>
        public TaskModuleMessageResponse(string type = default(string), string value = default(string))
            : base(type)
        {
            Value = value;
        }

        /// <summary>
        /// Gets or sets teams will display the value of value in a popup
        /// message box.
        /// </summary>
        [Newtonsoft.Json.JsonProperty(PropertyName = "value")]
        public string Value { get; set; }

    }

    /// <summary>
    /// Task Module Response with continue action.
    /// </summary>
    public partial class TaskModuleContinueResponse : TaskModuleResponseBase
    {
        /// <summary>
        /// Initializes a new instance of the TaskModuleContinueResponse class.
        /// </summary>
        public TaskModuleContinueResponse() { }

        /// <summary>
        /// Initializes a new instance of the TaskModuleContinueResponse class.
        /// </summary>
        /// <param name="type">Choice of action options when responding to the
        /// task/submit message. Possible values include: 'message',
        /// 'continue'</param>
        /// <param name="value">The JSON for the Adaptive card to appear in
        /// the task module.</param>
        public TaskModuleContinueResponse(string type = default(string), TaskModuleTaskInfo value = default(TaskModuleTaskInfo))
            : base(type)
        {
            Value = value;
        }

        /// <summary>
        /// Gets or sets the JSON for the Adaptive card to appear in the task
        /// module.
        /// </summary>
        [Newtonsoft.Json.JsonProperty(PropertyName = "value")]
        public TaskModuleTaskInfo Value { get; set; }

    }
}