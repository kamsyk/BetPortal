using System.Data;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.MSSqlServer;

namespace BetPortalWebApi.Logging
{
    public static class SerilogConfiguration
    {
        public const string DefaultTableName = "BpAppLogs";

        /// <summary>
        /// Reads levels from the <c>Serilog</c> section and writes events at or above <c>DatabaseLogging:MinimumLevel</c>
        /// (default Warning) to the table created by migration <c>AddAppLogTable</c>.
        /// </summary>
        public static void Configure(LoggerConfiguration loggerConfiguration, IConfiguration configuration)
        {
            loggerConfiguration
                .ReadFrom.Configuration(configuration)

                // GlobalExceptionHandler logs unhandled exceptions with user context; avoid a duplicate entry.
                .MinimumLevel.Override("Microsoft.AspNetCore.Diagnostics.ExceptionHandlerMiddleware", LogEventLevel.Fatal)
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .WriteTo.Debug();

            var section = configuration.GetSection("DatabaseLogging");
            var connectionString = configuration.GetConnectionString("BetPortalDbConnectionString");
            if (!section.GetValue("Enabled", true) || string.IsNullOrWhiteSpace(connectionString))
            {
                return;
            }

            loggerConfiguration.WriteTo.MSSqlServer(
                connectionString,
                new MSSqlServerSinkOptions
                {
                    TableName = section["TableName"] ?? DefaultTableName,
                    SchemaName = "dbo",
                    AutoCreateSqlTable = false,
                    BatchPostingLimit = 100,
                    BatchPeriod = TimeSpan.FromSeconds(section.GetValue("BatchPeriodSeconds", 5d)),
                },
                restrictedToMinimumLevel: section.GetValue("MinimumLevel", LogEventLevel.Warning),
                columnOptions: CreateColumnOptions());
        }

        private static ColumnOptions CreateColumnOptions()
        {
            var columnOptions = new ColumnOptions();
            columnOptions.Store.Remove(StandardColumn.Properties);
            columnOptions.Store.Add(StandardColumn.LogEvent);
            columnOptions.Store.Add(StandardColumn.TraceId);
            columnOptions.Store.Add(StandardColumn.SpanId);
            columnOptions.Id.DataType = SqlDbType.BigInt;
            columnOptions.Level.DataLength = 16;
            columnOptions.TimeStamp.DataType = SqlDbType.DateTime2;
            columnOptions.TimeStamp.ConvertToUtc = true;
            columnOptions.TraceId.DataLength = 32;
            columnOptions.SpanId.DataLength = 16;
            columnOptions.LogEvent.ExcludeStandardColumns = true;
            columnOptions.LogEvent.ExcludeAdditionalProperties = true;
            columnOptions.AdditionalColumns =
            [
                new SqlColumn("SourceContext", SqlDbType.NVarChar, allowNull: true, dataLength: 256),
                new SqlColumn("RequestPath", SqlDbType.NVarChar, allowNull: true, dataLength: 2048),
                new SqlColumn("UserId", SqlDbType.Int, allowNull: true),
            ];
            return columnOptions;
        }
    }
}
