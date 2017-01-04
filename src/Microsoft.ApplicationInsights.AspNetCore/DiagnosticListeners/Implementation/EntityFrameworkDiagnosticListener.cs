﻿namespace Microsoft.ApplicationInsights.AspNetCore.DiagnosticListeners.Implementation
{
    using System;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.Extensions.DiagnosticAdapter;

    /// <summary>
    /// <see cref="IApplicationInsightDiagnosticListener"/> implementation that listens for events specific to EntiryFrameworkCore.
    /// </summary>
    public class EntityFrameworkDiagnosticListener : IApplicationInsightDiagnosticListener
    {
        private readonly TelemetryClient client;
        private readonly ContextData<long> beginDependencyTimestamp = new ContextData<long>();

        /// <inheritdoc />
        public string ListenerName { get; } = "Microsoft.EntityFrameworkCore";

        /// <summary>
        /// Initializes a new instance of the <see cref="T:HostingDiagnosticListener"/> class.
        /// </summary>
        /// <param name="client"><see cref="TelemetryClient"/> to post traces to.</param>
        public EntityFrameworkDiagnosticListener(TelemetryClient client)
        {
            this.client = client;
        }

        /// <summary>
        /// Diagnostic event handler method for 'Microsoft.EntityFrameworkCore.BeforeExecuteCommand' event
        /// </summary>
        [DiagnosticName("Microsoft.EntityFrameworkCore.BeforeExecuteCommand")]
        public void OnBeginCommand(long timestamp)
        {
            beginDependencyTimestamp.Value = timestamp;
        }

        /// <summary>
        /// Diagnostic event handler method for 'Microsoft.EntityFrameworkCore.AfterExecuteCommand' event
        /// </summary>
        [DiagnosticName("Microsoft.EntityFrameworkCore.AfterExecuteCommand")]
        public void OnEndCommand(IDbCommand command, string executeMethod, Guid instanceId, long timestamp, bool isAsync)
        {
            var start = beginDependencyTimestamp.Value;
            var end = timestamp;

            var telemetry = new DependencyTelemetry();
            telemetry.Name = command.Connection.Database;
            telemetry.Data = command.CommandText;
            telemetry.Duration = new TimeSpan(end - start);
            telemetry.Timestamp = DateTimeOffset.Now - telemetry.Duration;
            telemetry.Target = command.Connection.Database;
            telemetry.Type = "SQL";
            client.TrackDependency(telemetry);
        }

        /// <summary>
        /// Proxy interface for <c>IDbCommand</c> from System.Data
        /// </summary>
        public interface IDbCommand
        {
            string CommandText { get; }

            IDbConnection Connection { get; }
        }

        /// <summary>
        /// Proxy interface for <c>IDbConnection</c> from System.Data
        /// </summary>
        public interface IDbConnection
        {
            string Database { get; }
        }
    }

}
