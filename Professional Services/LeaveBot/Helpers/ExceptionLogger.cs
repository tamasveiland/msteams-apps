using System;
using System.Web.Http.Filters;

namespace ProfessionalServices.LeaveBot.Helpers
{
    //common service to be used for logging errors
    public static class ErrorLogService
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