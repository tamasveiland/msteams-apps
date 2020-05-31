﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http.Filters;

namespace Airlines.XAirlines.Common
{
    public class ErrorLogService
    {
        public static void LogError(Exception ex)
        {
            var telemetry = new Microsoft.ApplicationInsights.TelemetryClient();
            telemetry.TrackException(ex);
            //Email developers, call fire department, log to database etc.
        }
    }
    //Create filter
    public class LogExceptionFilterAttribute : ExceptionFilterAttribute
    {
        public override void OnException(HttpActionExecutedContext context)
        {
            ErrorLogService.LogError(context.Exception);
        }
    }
}