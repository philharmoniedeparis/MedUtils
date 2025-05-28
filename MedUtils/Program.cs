using MedUtils.Features.Syracuse;
//tests
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/", () => "Hello les savoirs");

// Register all Syracuse endpoints
app.MapSyracuseEndpoints();

app.Run();


