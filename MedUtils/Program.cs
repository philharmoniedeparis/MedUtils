using MARC;
using MedUtils.Tools;
using System.Xml.Linq;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/", () => "Hello les savoirs");

app.MapGet("/GetRecordFromIdDocnum/{idDocnum}", async (string idDocnum) =>
{
    string xmlData = await Tools.getSyracuseRecordFromIdDocnum(idDocnum);
    return Results.Content(xmlData, "application/xml");
});

app.MapGet("/GetRecordFromIdSyracuse/{idSyracuse}", async (string idSyracuse) =>
{
    string xmlData = await Tools.getSyracuseRecordFromId(idSyracuse);
    return Results.Content(xmlData, "application/xml");
});


app.Run();


