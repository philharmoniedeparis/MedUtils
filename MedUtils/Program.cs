using MARC;
using MedUtils.Tools;
using System.Xml.Linq;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/", () => "Hello les savoirs");

app.MapGet("/GetRecordFromId/{id}", async (string id) =>
{
    string xmlData = await SyracuseTools.getRecordFromId(id);
    return Results.Content(xmlData, "application/xml");
});

app.MapGet("/GetRecordFromIdDocnum/{idDocnum}", async (string idDocnum) =>
{
    string xmlData = await SyracuseTools.getRecordFromIdDocnum(idDocnum);
    return Results.Content(xmlData, "application/xml");
});


app.MapGet("/GetValuesFromRecord/{id}/{field}/{subfield}", async (string id, string field, char subfield) =>
{
    List<string> result = await MarcDataField.getValues(id, field, subfield);
    return Results.Json(result);
});
app.Run();


