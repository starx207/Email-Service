using EmailService.Api.Endpoints;
using EmailService.Api.IoC;
using ServiceRegistryModules;

var builder = WebApplication.CreateBuilder(args);

builder.ApplyRegistries(config => config
    .OfTypes(typeof(EmailConfigurationRegistry))
);

var app = builder.Build();

// Define a minimal API endpoint
app.MapEndpoints();
app.MapGet("/", () => "Welcome to EmailService API!");

app.Run();
