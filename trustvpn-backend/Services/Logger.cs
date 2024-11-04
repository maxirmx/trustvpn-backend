    // Copyright (C) 2024 Maxim [maxirmx] Samsonov (www.sw.consulting)
    // All rights reserved.
    // This file is a part of OiltrackGateway applcation
    //
    // Redistribution and use in source and binary forms, with or without
    // modification, are permitted provided that the following conditions
    // are met:
    // 1. Redistributions of source code must retain the above copyright
    // notice, this list of conditions and the following disclaimer.
    // 2. Redistributions in binary form must reproduce the above copyright
    // notice, this list of conditions and the following disclaimer in the
    // documentation and/or other materials provided with the distribution.
    //
    // THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
    // ``AS IS'' AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED
    // TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR
    // PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDERS OR CONTRIBUTORS
    // BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
    // CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
    // SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
    // INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
    // CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
    // ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
    // POSSIBILITY OF SUCH DAMAGE.

    using System.Text;
    using Serilog;
    using Serilog.Events;
    using Serilog.Filters;
    using Serilog.Sinks.SystemConsole.Themes;
    using TrustVpn.Settings;

    namespace TrustVpn.Services;

public static class LoggerBootstrapper
{
    public static Serilog.ILogger GetSerilogLogger(IConfiguration configuration)
    {
        var loggerConfig = configuration
            .GetSection("Logging:Settings")
            .Get<LoggerSettings>();

        var loggerConfiguration = new LoggerConfiguration()
            .Enrich.FromLogContext();

        if (loggerConfig?.EntityWriteToFile == true)
        {
            loggerConfiguration
                .WriteTo.Logger(lc =>
                {
                    lc
                        .Filter.ByIncludingOnly(Matching.FromSource("Microsoft.EntityFrameworkCore"))
                        .Filter.ByIncludingOnly(Matching.FromSource("Npgsql"));
                    WriteToBigFile(lc);
                });
        }

        loggerConfiguration
            .WriteTo.Logger(lc =>
            {
                lc
                    .Filter.ByExcluding(Matching.FromSource("Microsoft.EntityFrameworkCore"))
                    .Filter.ByExcluding(Matching.FromSource("Npgsql"));
                if (loggerConfig?.WriteToConsole == true)
                {
                    WriteToConsole(lc);
                }
                if (loggerConfig?.WriteToDebug == true)
                {
                    WriteToDebug(lc);
                }
                if (loggerConfig?.WriteToFile == true)
                {
                    WriteToFile(lc);
                }
            })
            .ReadFrom.Configuration(configuration)
            .Destructure.ByTransforming<Exception>(ex => new { ex.Message, ex.StackTrace })
            .Destructure.ByTransforming<HttpRequest>(req => new { req.Method, req.Path, req.QueryString, req.Headers })
            .Destructure.ByTransforming<HttpResponse>(res => new { res.StatusCode, res.Headers });

        return loggerConfiguration
            .CreateLogger();
    }

    private static void WriteToFile(LoggerConfiguration loggerConfiguration)
    {
        loggerConfiguration
            .WriteTo.Logger(l => l.Filter.ByIncludingOnly(e => e.Level == LogEventLevel.Verbose)
                .WriteTo.Async(f => f
                    .File(
                        "./logs/verbose/app-log-.txt",
                        encoding: Encoding.UTF8,
                        outputTemplate:
                        "{Timestamp:o} [{Level:u3}] ({Application}/{MachineName}/{ThreadId}) {Message}{NewLine}{Exception}",
                        rollingInterval: RollingInterval.Day,
                        shared: true,
                        formatProvider: null
                    )))
            .WriteTo.Logger(l => l.Filter.ByIncludingOnly(e => e.Level == LogEventLevel.Debug)
                .WriteTo.Async(f => f
                    .File(
                        "./logs/debug/app-log-.txt",
                        encoding: Encoding.UTF8,
                        outputTemplate:
                        "{Timestamp:o} [{Level:u3}] ({Application}/{MachineName}/{ThreadId}) {Message}{NewLine}{Exception}",
                        rollingInterval: RollingInterval.Day,
                        shared: true,
                        formatProvider: null
                    )))
            .WriteTo.Logger(l => l.Filter.ByIncludingOnly(e => e.Level == LogEventLevel.Information)
                .WriteTo.Async(f => f
                    .File(
                        "./logs/info/app-log-.txt",
                        encoding: Encoding.UTF8,
                        outputTemplate:
                        "{Timestamp:o} [{Level:u3}] ({Application}/{MachineName}/{ThreadId}) {Message}{NewLine}{Exception}",
                        rollingInterval: RollingInterval.Day,
                        shared: true,
                        formatProvider: null
                    )))
            .WriteTo.Logger(l => l.Filter.ByIncludingOnly(e => e.Level == LogEventLevel.Warning)
                .WriteTo.Async(f => f
                    .File(
                        "./logs/warning/app-log-.txt",
                        encoding: Encoding.UTF8,
                        outputTemplate:
                        "{Timestamp:o} [{Level:u3}] ({Application}/{MachineName}/{ThreadId}) {Message}{NewLine}{Exception}",
                        rollingInterval: RollingInterval.Day,
                        shared: true,
                        formatProvider: null
                    )))
            .WriteTo.Logger(l => l.Filter.ByIncludingOnly(e => e.Level == LogEventLevel.Error)
                .WriteTo.Async(f => f
                    .File(
                        "./logs/error/app-log-.txt",
                        encoding: Encoding.UTF8,
                        outputTemplate:
                        "{Timestamp:o} [{Level:u3}] ({Application}/{MachineName}/{ThreadId}) {Message}{NewLine}{Exception}",
                        rollingInterval: RollingInterval.Day,
                        shared: true,
                        formatProvider: null
                    )))
            .WriteTo.Logger(l => l.Filter.ByIncludingOnly(e => e.Level == LogEventLevel.Fatal)
                .WriteTo.Async(f => f
                    .File(
                        "./logs/fatal/app-log-.txt",
                        encoding: Encoding.UTF8,
                        outputTemplate:
                        "{Timestamp:o} [{Level:u3}] ({Application}/{MachineName}/{ThreadId}) {Message}{NewLine}{Exception}",
                        rollingInterval: RollingInterval.Day,
                        shared: true,
                        formatProvider: null
                    )));
    }

    private static void WriteToDebug(LoggerConfiguration loggerConfiguration)
    {
        loggerConfiguration
            .WriteTo.Debug(
                outputTemplate:
                "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} <s:{SourceContext}>{NewLine}{Exception}"
            );
    }

    private static void WriteToConsole(LoggerConfiguration loggerConfiguration)
    {
        loggerConfiguration
            .WriteTo.Console(
                theme: AnsiConsoleTheme.Code,
                outputTemplate:
                "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} <s:{SourceContext}>{NewLine}{Exception}"
            );
    }

    private static void WriteToBigFile(LoggerConfiguration loggerConfiguration)
    {
        loggerConfiguration
            .WriteTo.Async(a => a
                .File(
                    "./logs/db/db-log-.txt",
                    encoding: Encoding.UTF8,
                    outputTemplate:
                    "{Timestamp:o} [{Level:u3}] ({Application}/{MachineName}/{ThreadId}) {Message}{NewLine}{Exception}",
                    fileSizeLimitBytes: 104857600,
                    rollOnFileSizeLimit: true,
                    rollingInterval: RollingInterval.Day,
                    shared: true,
                    formatProvider: null
                )
            );
    }
}
