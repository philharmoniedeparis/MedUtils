var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/", () => "Hello World!");
string accessKey = "85kxBW8Lm2vQi9";

//Return Complete MARC Record From IdDocnum 

app.MapGet("/GetRecordFromIdDocnum/{idDocnum}", async (string idDocnum) =>
{
    using var httpClient = new HttpClient();
    try
    {
        string apiUrl = "https://sigb.philharmoniedeparis.fr/sdk/documentService.svc/AdvancedFindDocuments";

        var requestData = new
        {
            query = $"IDOCNUM856B={idDocnum}",
            start = 0,
            limit = 10,
            sort = "ANPA",
            sortAscending = true
        };
        var content = new StringContent(
            System.Text.Json.JsonSerializer.Serialize(requestData),
            System.Text.Encoding.UTF8,
            "application/json");

        httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        httpClient.DefaultRequestHeaders.Add("accessKey", accessKey);
        var response = await httpClient.PostAsync(apiUrl, content);
        response.EnsureSuccessStatusCode();

        var jsonResponse = await response.Content.ReadAsStringAsync();
        // Parse the JSON to extract just the XML content
        using var jsonDoc = System.Text.Json.JsonDocument.Parse(jsonResponse);
        var root = jsonDoc.RootElement;

        if (root.TryGetProperty("d", out var d) &&
            d.TryGetProperty("results", out var results) &&
            results.GetArrayLength() > 0)
        {
            // Get the XML string from the first result
            string xmlData = results[0].GetString();

            // Return just the XML content
            return Results.Content(xmlData, "application/xml");
        }

        return Results.NotFound("No XML data found in the response");

        // If you want to return the entire JSON response instead, uncomment the line below
        //return Results.Content(jsonResponse, "application/json");
    }
    catch (HttpRequestException ex)
    {
        // Handle HTTP request errors
        return Results.Problem($"Error fetching data: {ex.Message}");
    }
});

app.Run();


