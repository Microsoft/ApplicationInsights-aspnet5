﻿//-----------------------------------------------------------------------
// <copyright file="AspNetEventSource.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace Microsoft.ApplicationInsights.AspNet.Extensibility.Implementation.Tracing
{
    using System;
    using System.Diagnostics.Tracing;

    /// <summary>
    /// Event source for Application Insights ASP.NET 5 SDK.
    /// </summary>
    [EventSource(Name = "Microsoft-ApplicationInsights-AspNetCore")]
    internal sealed class AspNetEventSource : EventSource
    {
        /// <summary>
        /// The singleton instance of this event source.
        /// Due to how EventSource initialization works this has to be a public field and not
        /// a property otherwise the internal state of the event source will not be enabled.
        /// </summary>
        public static readonly AspNetEventSource Instance = new AspNetEventSource();

        /// <summary>
        /// Prevents a default instance of the AspNetEventSource class from being created.
        /// </summary>
        private AspNetEventSource() : base()
        {
            try
            {
                this.ApplicationName = Microsoft.Extensions.PlatformAbstractions.PlatformServices.Default.Application.ApplicationName;
            }
            catch (Exception exp)
            {
                this.ApplicationName = "Undefined " + exp.Message;
            }
        }

        /// <summary>
        /// Gets the application name for use in logging events.
        /// </summary>
        public string ApplicationName { [NonEvent] get; [NonEvent]private set; }

        /// <summary>
        /// Logs an event for the an exception in the TelemetryInitializerBase Initialize method.
        /// </summary>
        /// <param name="errorMessage">The error message to write an event for.</param>
        /// <param name="appDomainName">An ignored placeholder to make EventSource happy.</param>
        [Event(1, Message = "{0}", Level = EventLevel.Error, Keywords = Keywords.Diagnostics)]
        public void LogTelemetryInitializerBaseInitializeException(string errorMessage, string appDomainName = "Incorrect")
        {
            this.WriteEvent(1, errorMessage, this.ApplicationName);
        }

        /// <summary>
        /// Logs an event for the TelemetryInitializerBase Initialize method when the HttpContext is null.
        /// </summary>
        /// <param name="appDomainName">An ignored placeholder to make EventSource happy.</param>
        [Event(2, Message = "TelemetryInitializerBase.Initialize - httpContextAccessor.HttpContext is null, returning.", Level = EventLevel.Warning, Keywords = Keywords.Diagnostics)]
        public void LogTelemetryInitializerBaseInitializeContextNull(string appDomainName = "Incorrect")
        {
            this.WriteEvent(2, this.ApplicationName);
        }

        /// <summary>
        /// Logs an event for the TelemetryInitializerBase Initialize method when RequestServices is null.
        /// </summary>
        /// <param name="appDomainName">An ignored placeholder to make EventSource happy.</param>
        [Event(3, Message = "TelemetryInitializerBase.Initialize - context.RequestServices is null, returning.", Level = EventLevel.Warning, Keywords = Keywords.Diagnostics)]
        public void LogTelemetryInitializerBaseInitializeRequestServicesNull(string appDomainName = "Incorrect")
        {
            this.WriteEvent(3, this.ApplicationName);
        }

        /// <summary>
        /// Logs an event for the TelemetryInitializerBase Initialize method when the request is null.
        /// </summary>
        /// <param name="appDomainName">An ignored placeholder to make EventSource happy.</param>
        [Event(4, Message = "TelemetryInitializerBase.Initialize - request is null, returning.", Level = EventLevel.Warning, Keywords = Keywords.Diagnostics)]
        public void LogTelemetryInitializerBaseInitializeRequestNull(string appDomainName = "Incorrect")
        {
            this.WriteEvent(4, this.ApplicationName);
        }

        /// <summary>
        /// Logs an event for the ClientIpHeaderTelemetryInitializer OnInitializeTelemetry method when the location IP is null.
        /// </summary>
        /// <param name="appDomainName">An ignored placeholder to make EventSource happy.</param>
        [Event(5, Message = "ClientIpHeaderTelemetryInitializer.OnInitializeTelemetry - telemetry.Context.Location.Ip is already set, returning.", Level = EventLevel.Warning, Keywords = Keywords.Diagnostics)]
        public void LogClientIpHeaderTelemetryInitializerOnInitializeTelemetryIpNull(string appDomainName = "Incorrect")
        {
            this.WriteEvent(5, this.ApplicationName);
        }

        /// <summary>
        /// Logs an event for the WebSessionTelemetryInitializer OnInitializeTelemetry method when the session Id is null.
        /// </summary>
        /// <param name="appDomainName">An ignored placeholder to make EventSource happy.</param>
        [Event(6, Message = "WebSessionTelemetryInitializer.OnInitializeTelemetry - telemetry.Context.Session.Id is null or empty, returning.", Level = EventLevel.Warning, Keywords = Keywords.Diagnostics)]
        public void LogWebSessionTelemetryInitializerOnInitializeTelemetrySessionIdNull(string appDomainName = "Incorrect")
        {
            this.WriteEvent(6, this.ApplicationName);
        }

        /// <summary>
        /// Logs an event for the WebUserTelemetryInitializer OnInitializeTelemetry method when the session Id is null.
        /// </summary>
        /// <param name="appDomainName">An ignored placeholder to make EventSource happy.</param>
        [Event(7, Message = "WebUserTelemetryInitializer.OnInitializeTelemetry - telemetry.Context.Session.Id is null or empty, returning.", Level = EventLevel.Warning, Keywords = Keywords.Diagnostics)]
        public void LogWebUserTelemetryInitializerOnInitializeTelemetrySessionIdNull(string appDomainName = "Incorrect")
        {
            this.WriteEvent(7, this.ApplicationName);
        }

        /// <summary>
        /// Logs an event for the ComponentVersionTelemetryInitializer constructor method when accessing project.json throws an exception.
        /// </summary>
        /// <param name="appDomainName">An ignored placeholder to make EventSource happy.</param>
        [Event(8, Message = "ComponentVersionTelemetryInitializer - acessing project.json failed.", Level = EventLevel.Warning, Keywords = Keywords.Diagnostics)]
        public void LogComponentVersionTelemetryInitializerFailsToAccessProjectJson(string appDomainName = "Incorrect")
        {
            this.WriteEvent(8, this.ApplicationName);
        }

        /// <summary>
        /// Logs an event for the SyntheticTelemetryInitializer OnInitializeTelemetry method when the SyntheticSource is already set
        /// </summary>
        /// <param name="appDomainName">An ignored placeholder to make EventSource happy.</param>
        [Event(9, Message = "SyntheticTelemetryInitializer.OnInitializeTelemetry - telemetry.Context.Operation.SyntheticSource is already set, returning.", Level = EventLevel.Warning, Keywords = Keywords.Diagnostics)]
        public void SyntheticTelemetryInitializerOnInitializeTelemetryHeaderNotPresent(string appDomainName = "Incorrect")
        {
            this.WriteEvent(9, this.ApplicationName);
        }

        /// <summary>
        /// Keywords for the AspNetEventSource.
        /// </summary>
        public sealed class Keywords
        {
            /// <summary>
            /// Keyword for errors that trace at Verbose level.
            /// </summary>
            public const EventKeywords Diagnostics = (EventKeywords)0x1;
        }
    }
}
