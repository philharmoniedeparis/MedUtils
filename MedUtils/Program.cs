using MedUtils.Features.CDN;
using MedUtils.Features.Deneb;
using MedUtils.Features.IAConferences;
using MedUtils.Features.Syracuse;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddScoped<CDNTools>();
var app = builder.Build();

app.MapGet("/", () => "Hello les savoirs");

app.MapSyracuseEndpoints();
app.MapIAConferencesEndPoints();
app.MapDenebEndpoints();
app.MapCDNEndpoints();

app.Run();


