using Zuijin.AspNetCore.DependencyInjection;
using Zuijin.AspNetCore.Endpoints;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Zuijin")
    ?? throw new InvalidOperationException("Connection string 'Zuijin' not found.");

builder.Services
    .AddZuijin(options =>
    {
        options.Issuer = builder.Configuration["Zuijin:Issuer"] ?? "https://localhost:5001";
    })
    .UseSqlServer(connectionString);

var app = builder.Build();

app.MapZuijinEndpoints();

app.Run();
