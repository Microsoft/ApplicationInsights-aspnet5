﻿namespace SampleWebAppIntegration.FunctionalTest
{
    using FunctionalTestUtils;
    using Xunit;

    public class TelemetryModuleWorkingMvcTests : TelemetryTestsBase
    {
        private const string assemblyName = "MVCFramework45.FunctionalTests";

        // The NET46 conditional check is wrapped inside the test to make the tests visible in the test explorer. We can move them to the class level once if the issue is resolved.

        [Fact]
        public void TestBasicDependencyPropertiesAfterRequestingBasicPage()
        {
#if NET46
            this.ValidateBasicDependency(assemblyName, "/Home/About/5", InProcessServer.UseApplicationInsights);
#endif
        }

        [Fact]
        public void TestIfPerformanceCountersAreCollected()
        {
#if NET46
            ValidatePerformanceCountersAreCollected(assemblyName, InProcessServer.UseApplicationInsights);
#endif
        }
    }
}
