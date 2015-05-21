﻿namespace Microsoft.ApplicationInsights.AspNet.Tests.TelemetryInitializers
{
    using System;

    using Microsoft.ApplicationInsights.AspNet.TelemetryInitializers;
    using Microsoft.ApplicationInsights.AspNet.Tests.Helpers;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.AspNet.Hosting;
    using Microsoft.AspNet.Http.Core;
    using Xunit;

    public class WebSessionTelemetryInitializerTests
    {
        [Fact]
        public void InitializeThrowIfHttpContextAccessorIsNull()
        {
            Assert.Throws<ArgumentNullException>(() => { var initializer = new WebSessionTelemetryInitializer(null, null); });
        }

        [Fact]
        public void InitializeDoesNotThrowIfHttpContextIsUnavailable()
        {
            var ac = new HttpContextAccessor() { HttpContext = null };

            var initializer = new WebSessionTelemetryInitializer(ac, new Tracing.AspNet5EventSource());

            initializer.Initialize(new RequestTelemetry());
        }

        [Fact]
        public void InitializeDoesNotThrowIfRequestTelemetryIsUnavailable()
        {
            var ac = new HttpContextAccessor() { HttpContext = new DefaultHttpContext() };

            var initializer = new WebSessionTelemetryInitializer(ac, new Tracing.AspNet5EventSource());

            initializer.Initialize(new RequestTelemetry());
        }

        [Fact]
        public void InitializeSetsSessionFromCookie()
        {
            var requestTelemetry = new RequestTelemetry();
            var contextAccessor = HttpContextAccessorHelper.CreateHttpContextAccessor(requestTelemetry);
            contextAccessor.HttpContext.Request.Headers["Cookie"] = "ai_session=test|2015-04-10T17:11:38.378Z|2015-04-10T17:11:39.180Z";
            var initializer = new WebSessionTelemetryInitializer(contextAccessor, new Tracing.AspNet5EventSource());

            initializer.Initialize(requestTelemetry);

            Assert.Equal("test", requestTelemetry.Context.Session.Id);
        }
       
        [Fact]
        public void InitializeDoesNotOverrideSessionProvidedInline()
        {
            var requestTelemetry = new RequestTelemetry();
            requestTelemetry.Context.Session.Id = "Inline";
            var contextAccessor = HttpContextAccessorHelper.CreateHttpContextAccessor(requestTelemetry);
            contextAccessor.HttpContext.Request.Headers["Cookie"] = "ai_session=test|2015-04-10T17:11:38.378Z|2015-04-10T17:11:39.180Z";
            var initializer = new WebSessionTelemetryInitializer(contextAccessor, null);

            initializer.Initialize(requestTelemetry);

            Assert.Equal("Inline", requestTelemetry.Context.Session.Id);
        }
    }
}