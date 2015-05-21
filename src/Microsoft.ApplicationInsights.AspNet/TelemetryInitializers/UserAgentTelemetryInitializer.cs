﻿namespace Microsoft.ApplicationInsights.AspNet.TelemetryInitializers
{
    using System;

    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.AspNet.Hosting;
    using Microsoft.AspNet.Http;
    using Microsoft.Net.Http.Headers;
    using Microsoft.ApplicationInsights.AspNet.Tracing;


    /// <summary>
    /// Telemetry initializer populates user agent (telemetry.Context.User.UserAgent) for 
    /// all telemetry data items.
    /// </summary>
    public class UserAgentTelemetryInitializer : TelemetryInitializerBase
    {
        public UserAgentTelemetryInitializer(IHttpContextAccessor httpContextAccessor, AspNet5EventSource eventSource)
             : base(httpContextAccessor, eventSource)
        {
        }

        protected override void OnInitializeTelemetry(HttpContext platformContext, RequestTelemetry requestTelemetry, ITelemetry telemetry)
        {
            // TODO: conside using
            // var connectionFeature = platformContext.GetFeature<HttpRequestFeature>();
            // connectionFeature.Headers
            
            if (string.IsNullOrEmpty(telemetry.Context.User.UserAgent))
            {
                telemetry.Context.User.UserAgent = platformContext.Request.Headers[HeaderNames.UserAgent];
            }            
        }
    }
}