﻿namespace Microsoft.ApplicationInsights.AspNet.TelemetryInitializers
{
    using System;
    using System.Globalization;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.AspNet.Hosting;
    using Microsoft.AspNet.Http;
    using Microsoft.ApplicationInsights.AspNet.Tracing;

    public class WebUserTelemetryInitializer : TelemetryInitializerBase
    {
        private const string WebUserCookieName = "ai_user";

        private readonly AspNet5EventSource eventSource;

        public WebUserTelemetryInitializer(IHttpContextAccessor httpContextAccessor, AspNet5EventSource eventSource)
             : base(httpContextAccessor, eventSource)
        {
            this.eventSource = eventSource;
        }

        protected override void OnInitializeTelemetry(HttpContext platformContext, RequestTelemetry requestTelemetry, ITelemetry telemetry)
        {
            if (!string.IsNullOrEmpty(telemetry.Context.User.Id))
            {
                return;
            }

            if (string.IsNullOrEmpty(requestTelemetry.Context.User.Id))
            {
                UpdateRequestTelemetryFromPlatformContext(requestTelemetry, platformContext);
            }

            if (!string.IsNullOrEmpty(requestTelemetry.Context.User.Id))
            {
                telemetry.Context.User.Id = requestTelemetry.Context.User.Id;
            }

            if (requestTelemetry.Context.User.AcquisitionDate.HasValue)
            {
                telemetry.Context.User.AcquisitionDate = requestTelemetry.Context.User.AcquisitionDate;
            }
        }

        private void UpdateRequestTelemetryFromPlatformContext(RequestTelemetry requestTelemetry, HttpContext platformContext)
        {
            if (platformContext.Request.Cookies != null && platformContext.Request.Cookies.ContainsKey(WebUserCookieName))
            {
                var userCookieValue = platformContext.Request.Cookies[WebUserCookieName];
                bool cookieWasRead = false;

                if (!string.IsNullOrEmpty(userCookieValue))
                {
                    var userCookieParts = userCookieValue.Split('|');
                    if (userCookieParts.Length >= 2)
                    {
                        DateTimeOffset acquisitionDate = DateTimeOffset.MinValue;
                        if (!string.IsNullOrEmpty(userCookieParts[1]) 
                            && DateTimeOffset.TryParse(userCookieParts[1], CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out acquisitionDate))
                        {
                            cookieWasRead = true;
                            requestTelemetry.Context.User.Id = userCookieParts[0];
                            requestTelemetry.Context.User.AcquisitionDate = acquisitionDate;
                        }
                    }
                }

                if (!cookieWasRead)
                {
                    this.eventSource.MalformedCookie(WebUserCookieName, userCookieValue);
                }
            }
        }
    }
}