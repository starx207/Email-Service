var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Define a minimal API endpoint
app.MapGet("/", () => "Welcome to EmailService API!");

app.Run();
