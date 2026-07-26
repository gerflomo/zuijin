using Zuijin.Application.Configuration;
using Zuijin.AspNetCore.DependencyInjection;
using Zuijin.AspNetCore.Endpoints;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Zuijin")
    ?? throw new InvalidOperationException(
        "Connection string 'Zuijin' not found. Configure it via user secrets (local) or a secret store (cloud).");

builder.Services
    .AddZuijin(options => builder.Configuration.GetSection(ZuijinOptions.SectionName).Bind(options))
    .UseSqlServer(connectionString);

var app = builder.Build();

app.MapZuijinEndpoints();

app.Run();

/// <summary>
/// Exposed so integration tests can boot the real host through WebApplicationFactory.
/// </summary>
public partial class Program;
