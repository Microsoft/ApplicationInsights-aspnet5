﻿namespace SampleWebAppIntegration.FunctionalTest
{
    using System;
    using FunctionalTestUtils;
    using Microsoft.ApplicationInsights.DataContracts;
    using Xunit;
    using Microsoft.ApplicationInsights.AspNet.Tests.Helpers;
    using Xunit.Abstractions;

    public class ExceptionTelemetryTests : TelemetryTestsBase
    {
        public ExceptionTelemetryTests()
        {
        }

        private const string assemblyName = "Mvc6Framework45.FunctionalTests";

        [Fact]
        public void TestBasicRequestPropertiesAfterRequestingControllerThatThrows()
        {
            using (var server = new InProcessServer(assemblyName))
            {
                const string RequestPath = "/Home/Exception";

                var expectedRequestTelemetry = new RequestTelemetry();
                expectedRequestTelemetry.HttpMethod = "GET";
                expectedRequestTelemetry.Name = "GET Home/Exception";
                expectedRequestTelemetry.ResponseCode = "500";
                expectedRequestTelemetry.Success = false;
                expectedRequestTelemetry.Url = new System.Uri(server.BaseHost + RequestPath);
                this.ValidateBasicRequest(server, "/Home/Exception", expectedRequestTelemetry);
            }
        }

        [Fact]
        public void TestBasicExceptionPropertiesAfterRequestingControllerThatThrows()
        {
            using (var server = new InProcessServer(assemblyName))
            {
                var expectedExceptionTelemetry = new ExceptionTelemetry();
                expectedExceptionTelemetry.HandledAt = ExceptionHandledAt.Platform;
                expectedExceptionTelemetry.Exception = new InvalidOperationException();

                this.ValidateBasicException(server, "/Home/Exception", expectedExceptionTelemetry);
            }
        }
    }
}
