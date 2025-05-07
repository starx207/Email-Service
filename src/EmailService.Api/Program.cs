using EmailService.Api.Endpoints;
using EmailService.Api.IoC;
using EmailService.Api.Middleware;
using ServiceRegistryModules;

var builder = WebApplication.CreateBuilder(args);

builder.ApplyRegistries(config => config
    .OfTypes(typeof(EmailConfigurationRegistry))
);

var app = builder.Build();

app.UseAppExceptionHandling();
app.MapEndpoints();
app.Run();
