// Copyright (C) 2023 Maxim [maxirmx] Samsonov (www.sw.consulting)
// All rights reserved.
// This file is a part of TrustVPN applcation
// Adopted from:
// https://dev.to/francescoxx/c-c-sharp-crud-rest-api-using-net-7-aspnet-entity-framework-postgres-docker-and-docker-compose-493a
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


using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Npgsql;

using TrustVpn.Authorization;
using TrustVpn.Data;
using TrustVpn.Extensions;
using TrustVpn.Services;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

// Added configuration for PostgreSQL
var configuration = builder.Configuration;
var connectionString = configuration.GetConnectionString("DefaultConnection");
using var connection = new NpgsqlConnection(connectionString);

builder.Services
    .AddCors()
// configure strongly typed settings object
    .Configure<AppSettings>(builder.Configuration.GetSection("AppSettings"))
// configure DI for application services
    .AddScoped<IJwtUtils, JwtUtils>()
    .AddHttpContextAccessor()
    .ConfigureLoggerServices(configuration)
    .AddSingleton<DbEnsure>()
    .AddSingleton<TrustVpnServiceContainer>()
    .AddSingleton<TrustVpnDbContainer>()
    .AddDbContext<ProfileContext>(options => options.UseNpgsql(connectionString))
    .AddDbContext<UserContext>(options =>options.UseNpgsql(connectionString))
    .AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo { Title = "TrustVpn Api", Version = "v1" });
        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme {
            Description = "JWT Authorization token. Example: \"Authorization: {token}\"",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.Http,
            Scheme = "bearer"
        });

        var scm = new OpenApiSecurityScheme
        {
            Reference = new OpenApiReference
            {
                Type = ReferenceType.SecurityScheme,
                Id = "Bearer"
            }
        };

        c.AddSecurityRequirement(new OpenApiSecurityRequirement { { scm, Array.Empty<string>() } });
    });

var app = builder.Build();

var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Starting the application");
while (true)
{
    try
    {
        connection.Open();
        break;
    }
    catch (Exception ex)
    {
        logger.LogError("Failed to create database connection: {msg}", ex.Message);
        Thread.Sleep(3000);
    }
}

app.Services.GetRequiredService<DbEnsure>().Ensure(connection); ;


// global cors policy
app
    .UseCors(x => x
        .AllowAnyOrigin()
        .AllowAnyMethod()
        .AllowAnyHeader()
    )
// custom jwt auth middleware
    .UseMiddleware<JwtMiddleware>();

// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
//{
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
// builder.Services.AddEndpointsApiExplorer();
// builder.Services.AddSwaggerGen();

app
    .UseSwagger()
    .UseSwaggerUI();
//}

app
    .UseHttpsRedirection()
    .UseAuthorization();

app.MapControllers();
app.Run();
