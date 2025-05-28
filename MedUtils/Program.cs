using MedUtils.Features.Syracuse;
using MedUtils.Features.Deneb;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

//IConfiguration configuration = new ConfigurationBuilder()
//    .AddJsonFile("appsettings.json")
//    .AddEnvironmentVariables()
//    .Build();

//IConfiguration DenebUser = configuration.GetSection("Deneb");

app.MapGet("/", () => "Hello les savoirs");
//app.MapGet("/", () => DenebUser.GetValue<string>("User",""));


// Register all Syracuse endpoints
app.MapSyracuseEndpoints();
app.MapDenebEndpoints();

app.Run();


