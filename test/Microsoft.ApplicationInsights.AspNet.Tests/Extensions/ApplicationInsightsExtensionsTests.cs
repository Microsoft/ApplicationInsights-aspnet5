﻿using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]
namespace Microsoft.Extensions.DependencyInjection.Test
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.AspNet.ContextInitializers;
    using Microsoft.ApplicationInsights.AspNet.TelemetryInitializers;
    using Microsoft.ApplicationInsights.AspNet.Tests;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.AspNet.Extensions;
    using Microsoft.AspNet.Http;
    using Microsoft.AspNet.Http.Internal;
    using Microsoft.Extensions.Configuration;

#if dnx451
    using ApplicationInsights.DependencyCollector;
    using ApplicationInsights.Extensibility.PerfCounterCollector;
    using ApplicationInsights.WindowsServer.TelemetryChannel;
#endif

    public static class ApplicationInsightsExtensionsTests
    {
        /// <summary>Constant instrumentation key value for testintg.</summary>
        public const string TestInstrumentationKey = "11111111-2222-3333-4444-555555555555";
        private const string InstrumentationKeyFromConfig = "ApplicationInsights:InstrumentationKey";

        public static ServiceCollection GetServiceCollectionWithContextAccessor()
        {
            var services = new ServiceCollection();
            IHttpContextAccessor contextAccessor = new HttpContextAccessor();
            services.AddInstance<IHttpContextAccessor>(contextAccessor);
            services.AddInstance<DiagnosticListener>(new DiagnosticListener("TestListener"));
            return services;
        }

        public static class AddApplicationInsightsTelemetry
        {
            [Theory]
            [InlineData(typeof(ITelemetryInitializer), typeof(DomainNameRoleInstanceTelemetryInitializer), ServiceLifetime.Singleton)]
            [InlineData(typeof(ITelemetryInitializer), typeof(ClientIpHeaderTelemetryInitializer), ServiceLifetime.Singleton)]
            [InlineData(typeof(ITelemetryInitializer), typeof(OperationNameTelemetryInitializer), ServiceLifetime.Singleton)]
            [InlineData(typeof(ITelemetryInitializer), typeof(OperationIdTelemetryInitializer), ServiceLifetime.Singleton)]
            [InlineData(typeof(ITelemetryInitializer), typeof(SyntheticTelemetryInitializer), ServiceLifetime.Singleton)]
            [InlineData(typeof(ITelemetryInitializer), typeof(UserAgentTelemetryInitializer), ServiceLifetime.Singleton)]
            [InlineData(typeof(ITelemetryInitializer), typeof(WebSessionTelemetryInitializer), ServiceLifetime.Singleton)]
            [InlineData(typeof(ITelemetryInitializer), typeof(WebUserTelemetryInitializer), ServiceLifetime.Singleton)]
            [InlineData(typeof(TelemetryConfiguration), null, ServiceLifetime.Singleton)]
            [InlineData(typeof(TelemetryClient), typeof(TelemetryClient), ServiceLifetime.Scoped)]
            public static void RegistersExpectedServices(Type serviceType, Type implementationType, ServiceLifetime lifecycle)
            {
                var services = CreateServicesAndAddApplicationinsightsTelemetry(null, null);
                ServiceDescriptor service = services.Single(s => s.ServiceType == serviceType && s.ImplementationType == implementationType);
                Assert.Equal(lifecycle, service.Lifetime);
            }

            [Fact]
            public static void DoesNotThrowWithoutInstrumentationKey()
            {
                var services = CreateServicesAndAddApplicationinsightsTelemetry(null, null);
            }

            [Fact]
            public static void RegistersTelemetryConfigurationFactoryMethodThatCreatesDefaultInstance()
            {
                var services = CreateServicesAndAddApplicationinsightsTelemetry(null, null);

                IServiceProvider serviceProvider = services.BuildServiceProvider();
                var telemetryConfiguration = serviceProvider.GetRequiredService<TelemetryConfiguration>();
                Assert.Contains(telemetryConfiguration.TelemetryInitializers, t => t is OperationIdTelemetryInitializer);
            }

            /// <summary>
            /// Tests that the instrumentation key configuration can be read from a JSON file by the configuration factory.
            /// </summary>
            [Fact]
            public static void RegistersTelemetryConfigurationFactoryMethodThatReadsInstrumentationKeyFromConfiguration()
            {
                var services = CreateServicesAndAddApplicationinsightsTelemetry("content\\config-instrumentation-key.json", null);

                IServiceProvider serviceProvider = services.BuildServiceProvider();
                var telemetryConfiguration = serviceProvider.GetRequiredService<TelemetryConfiguration>();
                Assert.Equal(TestInstrumentationKey, telemetryConfiguration.InstrumentationKey);
            }

            /// <summary>
            /// Tests that the Active configuration singleton is used as the telemetry configuration instance by the configuration factory.
            /// This demonstrates that existing documentation for how to create a telemetry client and track custom events etc. works in ASP.NET 5
            /// when no ApplicationInsights.config file exists but a project.json file does exist which contains the instrumentation key.
            /// </summary>
            [Fact]
            public static void ConfigurationFactoryMethodUpdatesTheActiveConfigurationSingletonByDefault()
            {
                var services = CreateServicesAndAddApplicationinsightsTelemetry("content\\config-instrumentation-key.json", null);

                IServiceProvider serviceProvider = services.BuildServiceProvider();
                TelemetryConfiguration telemetryConfiguration = serviceProvider.GetRequiredService<TelemetryConfiguration>();
                Assert.Equal(TestInstrumentationKey, TelemetryConfiguration.Active.InstrumentationKey);
            }

            [Fact]
            public static void RegistersTelemetryConfigurationFactoryMethodThatReadsDeveloperModeFromConfiguration()
            {
                var services = CreateServicesAndAddApplicationinsightsTelemetry("content\\config-developer-mode.json", null);

                IServiceProvider serviceProvider = services.BuildServiceProvider();
                var telemetryConfiguration = serviceProvider.GetRequiredService<TelemetryConfiguration>();
                Assert.Equal(true, telemetryConfiguration.TelemetryChannel.DeveloperMode);
            }

            [Fact]
            public static void RegistersTelemetryConfigurationFactoryMethodThatReadsEndpointAddressFromConfiguration()
            {
                var services = CreateServicesAndAddApplicationinsightsTelemetry("content\\config-endpoint-address.json", null);

                IServiceProvider serviceProvider = services.BuildServiceProvider();
                var telemetryConfiguration = serviceProvider.GetRequiredService<TelemetryConfiguration>();
                Assert.Equal("http://localhost:1234/v2/track/", telemetryConfiguration.TelemetryChannel.EndpointAddress);
            }

            [Fact]
            public static void RegistersTelemetryConfigurationFactoryMethodThatReadsInstrumentationKeyFromEnvironment()
            {
                var services = ApplicationInsightsExtensionsTests.GetServiceCollectionWithContextAccessor();
                services.AddInstance<ITelemetryChannel>(new InMemoryChannel());
                Environment.SetEnvironmentVariable("APPINSIGHTS_INSTRUMENTATIONKEY", TestInstrumentationKey);
                var config = new ConfigurationBuilder().AddEnvironmentVariables().Build();
                try
                {
                    services.AddApplicationInsightsTelemetry(config);

                    IServiceProvider serviceProvider = services.BuildServiceProvider();
                    var telemetryConfiguration = serviceProvider.GetRequiredService<TelemetryConfiguration>();
                    Assert.Equal(TestInstrumentationKey, telemetryConfiguration.InstrumentationKey);
                }
                finally
                {
                    Environment.SetEnvironmentVariable("APPINSIGHTS_INSTRUMENTATIONKEY", null);
                }
            }

            [Fact]
            public static void RegistersTelemetryConfigurationFactoryMethodThatReadsDeveloperModeFromEnvironment()
            {
                var services = ApplicationInsightsExtensionsTests.GetServiceCollectionWithContextAccessor();
                services.AddInstance<ITelemetryChannel>(new InMemoryChannel());
                Environment.SetEnvironmentVariable("APPINSIGHTS_DEVELOPER_MODE", "true");
                var config = new ConfigurationBuilder().AddEnvironmentVariables().Build();
                try
                {
                    services.AddApplicationInsightsTelemetry(config);

                    IServiceProvider serviceProvider = services.BuildServiceProvider();
                    var telemetryConfiguration = serviceProvider.GetRequiredService<TelemetryConfiguration>();
                    Assert.Equal(true, telemetryConfiguration.TelemetryChannel.DeveloperMode);
                }
                finally
                {
                    Environment.SetEnvironmentVariable("APPINSIGHTS_DEVELOPER_MODE", null);
                }
            }

            [Fact]
            public static void RegistersTelemetryConfigurationFactoryMethodThatReadsEndpointAddressFromEnvironment()
            {
                var services = ApplicationInsightsExtensionsTests.GetServiceCollectionWithContextAccessor();
                services.AddInstance<ITelemetryChannel>(new InMemoryChannel());
                Environment.SetEnvironmentVariable("APPINSIGHTS_ENDPOINTADDRESS", "http://localhost:1234/v2/track/");
                var config = new ConfigurationBuilder().AddEnvironmentVariables().Build();
                try
                {
                    services.AddApplicationInsightsTelemetry(config);

                    IServiceProvider serviceProvider = services.BuildServiceProvider();
                    var telemetryConfiguration = serviceProvider.GetRequiredService<TelemetryConfiguration>();
                    Assert.Equal("http://localhost:1234/v2/track/", telemetryConfiguration.TelemetryChannel.EndpointAddress);
                }
                finally
                {
                    Environment.SetEnvironmentVariable("APPINSIGHTS_ENDPOINTADDRESS", null);
                }
            }

            [Fact]
            public static void RegistersTelemetryConfigurationFactoryMethodThatPopulatesItWithTelemetryInitializersFromContainer()
            {
                var telemetryInitializer = new FakeTelemetryInitializer();
                var services = ApplicationInsightsExtensionsTests.GetServiceCollectionWithContextAccessor();
                services.AddInstance<ITelemetryChannel>(new InMemoryChannel());
                services.AddInstance<ITelemetryInitializer>(telemetryInitializer);

                services.AddApplicationInsightsTelemetry(new ConfigurationBuilder().Build());

                IServiceProvider serviceProvider = services.BuildServiceProvider();
                var telemetryConfiguration = serviceProvider.GetRequiredService<TelemetryConfiguration>();
                Assert.Contains(telemetryInitializer, telemetryConfiguration.TelemetryInitializers);
            }

            [Fact]
            public static void RegistersTelemetryConfigurationFactoryMethodThatPopulatesItWithTelemetryChannelFromContainer()
            {
                var telemetryChannel = new FakeTelemetryChannel();
                var services = ApplicationInsightsExtensionsTests.GetServiceCollectionWithContextAccessor();
                services.AddInstance<ITelemetryChannel>(telemetryChannel);

                services.AddApplicationInsightsTelemetry(new ConfigurationBuilder().Build());

                IServiceProvider serviceProvider = services.BuildServiceProvider();
                var telemetryConfiguration = serviceProvider.GetRequiredService<TelemetryConfiguration>();
                Assert.Same(telemetryChannel, telemetryConfiguration.TelemetryChannel);
            }

            [Fact]
            public static void DoesNotOverrideDefaultTelemetryChannelIfTelemetryChannelServiceIsNotRegistered()
            {
                var services = CreateServicesAndAddApplicationinsightsTelemetry(null, null);
                IServiceProvider serviceProvider = services.BuildServiceProvider();
                var telemetryConfiguration = serviceProvider.GetRequiredService<TelemetryConfiguration>();
                Assert.NotNull(telemetryConfiguration.TelemetryChannel);
            }

            [Fact]
            public static void RegistersTelemetryClientToGetTelemetryConfigurationFromContainerAndNotGlobalInstance()
            {
                ITelemetry sentTelemetry = null;
                var telemetryChannel = new FakeTelemetryChannel { OnSend = telemetry => sentTelemetry = telemetry };
                var services = CreateServicesAndAddApplicationinsightsTelemetry(null, null);
                IServiceProvider serviceProvider = services.BuildServiceProvider();
                var configuration = serviceProvider.GetRequiredService<TelemetryConfiguration>();
                configuration.InstrumentationKey = Guid.NewGuid().ToString();
                configuration.TelemetryChannel = telemetryChannel;

                var telemetryClient = serviceProvider.GetRequiredService<TelemetryClient>();
                telemetryClient.TrackEvent("myevent");

                // We want to check that configuration from contaier was used but configuration is a private field so we check
                // instrumentation key instead
                Assert.Equal(configuration.InstrumentationKey, sentTelemetry.Context.InstrumentationKey);
            }

            
            [Fact]
            public static void AddApplicationInsightsTelemetryDoesNotThrowOnNullServiceOptions()
            {
                var services = CreateServicesAndAddApplicationinsightsTelemetry(null, "http://localhost:1234/v2/track/", null, false);
                IServiceProvider serviceProvider = services.BuildServiceProvider();
                var telemetryConfiguration = serviceProvider.GetRequiredService<TelemetryConfiguration>();
            }


#if dnx451
            [Fact]
            public static void RegistersTelemetryConfigurationFactoryMethodThatPopulatesItWithModulesFromContainer()
            {
                var services = CreateServicesAndAddApplicationinsightsTelemetry(null, null, null, false);
                IServiceProvider serviceProvider = services.BuildServiceProvider();
                var modules = serviceProvider.GetServices<ITelemetryModule>();
                Assert.NotNull(modules);
                Assert.Equal(2, modules.Count());

                var dependencyModule = services.FirstOrDefault<ServiceDescriptor>(t => t.ImplementationType == typeof(DependencyTrackingTelemetryModule));
                Assert.NotNull(dependencyModule);

                var perfCounterModule = services.FirstOrDefault<ServiceDescriptor>(t => t.ImplementationType == typeof(PerformanceCollectorModule));
                Assert.NotNull(perfCounterModule);
            }

            [Fact]
            public static void AddsAddaptiveSamplingServiceToTheConfigurationInFullFrameworkByDefault()
            {
                var exisitingProcessorCount = GetTelemetryProcessorsCountInConfiguration<AdaptiveSamplingTelemetryProcessor>(TelemetryConfiguration.Active);
                var services = CreateServicesAndAddApplicationinsightsTelemetry(null, "http://localhost:1234/v2/track/", null, false);
                IServiceProvider serviceProvider = services.BuildServiceProvider();
                var telemetryConfiguration = serviceProvider.GetRequiredService<TelemetryConfiguration>();
                var updatedCount = GetTelemetryProcessorsCountInConfiguration<AdaptiveSamplingTelemetryProcessor>(telemetryConfiguration);
                Assert.Equal(updatedCount, exisitingProcessorCount + 1);
            }

            [Fact]
            public static void DoesNotAddSamplingToConfigurationIfExplicitlyControlledThroughParameter()
            {
                var exisitingProcessorCount = GetTelemetryProcessorsCountInConfiguration<AdaptiveSamplingTelemetryProcessor>(TelemetryConfiguration.Active);
                var serviceOptions = new ApplicationInsightsServiceOptions();
                serviceOptions.EnableAdaptiveSampling = false;
                var services = CreateServicesAndAddApplicationinsightsTelemetry(null, "http://localhost:1234/v2/track/", serviceOptions, false);
                IServiceProvider serviceProvider = services.BuildServiceProvider();
                var telemetryConfiguration = serviceProvider.GetRequiredService<TelemetryConfiguration>();
                var updatedCount = GetTelemetryProcessorsCountInConfiguration<AdaptiveSamplingTelemetryProcessor>(telemetryConfiguration);
                Assert.Equal(updatedCount, exisitingProcessorCount);
            }

            [Fact]
            public static void AddsAddaptiveSamplingServiceToTheConfigurationInFullFrameworkWithServiceOptions()
            {
                var exisitingProcessorCount = GetTelemetryProcessorsCountInConfiguration<AdaptiveSamplingTelemetryProcessor>(TelemetryConfiguration.Active);
                var serviceOptions = new ApplicationInsightsServiceOptions();
                serviceOptions.EnableAdaptiveSampling = true;
                var services = CreateServicesAndAddApplicationinsightsTelemetry(null, "http://localhost:1234/v2/track/", serviceOptions, false);
                IServiceProvider serviceProvider = services.BuildServiceProvider();
                var telemetryConfiguration = serviceProvider.GetRequiredService<TelemetryConfiguration>();
                var updatedCount =  GetTelemetryProcessorsCountInConfiguration<AdaptiveSamplingTelemetryProcessor>(telemetryConfiguration);
                Assert.Equal(updatedCount, exisitingProcessorCount + 1);
            }

            [Fact]
            public static void AddsServerTelemetryChannelInFullFramework()
            {
                var services = CreateServicesAndAddApplicationinsightsTelemetry(null, "http://localhost:1234/v2/track/", null, false);
                IServiceProvider serviceProvider = services.BuildServiceProvider();
                var telemetryConfiguration = serviceProvider.GetRequiredService<TelemetryConfiguration>();
                Assert.Equal(telemetryConfiguration.TelemetryChannel.GetType(), typeof(ServerTelemetryChannel));
            }

            [Fact]
            public static void DoesNotOverWriteExistingChannelInFullFramework()
            {
                var services = ApplicationInsightsExtensionsTests.GetServiceCollectionWithContextAccessor();
                services.AddSingleton<ITelemetryChannel, InMemoryChannel>();
                var config = new ConfigurationBuilder().AddApplicationInsightsSettings(endpointAddress: "http://localhost:1234/v2/track/").Build();
                services.AddApplicationInsightsTelemetry(config);

                IServiceProvider serviceProvider = services.BuildServiceProvider();
                var telemetryConfiguration = serviceProvider.GetRequiredService<TelemetryConfiguration>();
                Assert.Equal(telemetryConfiguration.TelemetryChannel.GetType(), typeof(InMemoryChannel));
            }
#endif
            private static int GetTelemetryProcessorsCountInConfiguration<T>(TelemetryConfiguration telemetryConfiguration)
            {
                return telemetryConfiguration.TelemetryProcessors.Where(processor => processor.GetType() == typeof(T)).Count();
            }
        }

        public static class AddApplicationInsightsSettings
        {
            [Fact]
            public static void RegistersTelemetryConfigurationFactoryMethodThatReadsInstrumentationKeyFromSettings()
            {
                var services = ApplicationInsightsExtensionsTests.GetServiceCollectionWithContextAccessor();
                services.AddInstance<ITelemetryChannel>(new InMemoryChannel());
                var config = new ConfigurationBuilder().AddApplicationInsightsSettings(instrumentationKey: TestInstrumentationKey).Build();
                services.AddApplicationInsightsTelemetry(config);

                IServiceProvider serviceProvider = services.BuildServiceProvider();
                var telemetryConfiguration = serviceProvider.GetRequiredService<TelemetryConfiguration>();
                Assert.Equal(TestInstrumentationKey, telemetryConfiguration.InstrumentationKey);
            }

            [Fact]
            public static void RegistersTelemetryConfigurationFactoryMethodThatReadsDeveloperModeFromSettings()
            {
                var services = ApplicationInsightsExtensionsTests.GetServiceCollectionWithContextAccessor();
                services.AddInstance<ITelemetryChannel>(new InMemoryChannel());
                var config = new ConfigurationBuilder().AddApplicationInsightsSettings(developerMode: true).Build();
                services.AddApplicationInsightsTelemetry(config);

                IServiceProvider serviceProvider = services.BuildServiceProvider();
                var telemetryConfiguration = serviceProvider.GetRequiredService<TelemetryConfiguration>();
                Assert.Equal(true, telemetryConfiguration.TelemetryChannel.DeveloperMode);
            }

            [Fact]
            public static void RegistersTelemetryConfigurationFactoryMethodThatReadsEndpointAddressFromSettings()
            {
                var services = CreateServicesAndAddApplicationinsightsTelemetry(null, "http://localhost:1234/v2/track/");
                IServiceProvider serviceProvider = services.BuildServiceProvider();
                var telemetryConfiguration = serviceProvider.GetRequiredService<TelemetryConfiguration>();
                Assert.Equal("http://localhost:1234/v2/track/", telemetryConfiguration.TelemetryChannel.EndpointAddress);
            }
        }
        public static ServiceCollection CreateServicesAndAddApplicationinsightsTelemetry(string jsonPath, string channelEndPointAddress, ApplicationInsightsServiceOptions serviceOptions = null, bool addChannel = true)
        {
            var services = ApplicationInsightsExtensionsTests.GetServiceCollectionWithContextAccessor();
            if (addChannel)
            {
                services.AddInstance<ITelemetryChannel>(new InMemoryChannel());
            }
            IConfigurationRoot config = null;

            if (jsonPath != null)
            {
                config = new ConfigurationBuilder().AddJsonFile(jsonPath).Build();
            }
            else  if (channelEndPointAddress != null)
            {
                config = new ConfigurationBuilder().AddApplicationInsightsSettings(endpointAddress: channelEndPointAddress).Build();
            }
            else
            {
                config = new ConfigurationBuilder().Build();
            }

             services.AddApplicationInsightsTelemetry(config, serviceOptions);
             return services;
        }
    }
}