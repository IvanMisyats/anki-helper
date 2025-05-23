using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using OpenAI;
using OpenAI.Chat;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Configure OpenAI client
var openAiApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") 
                   ?? builder.Configuration["OpenAI:ApiKey"];

if (string.IsNullOrEmpty(openAiApiKey))
{
    throw new Exception("OpenAI API Key not found. Please set the OPENAI_API_KEY environment variable.");
}

builder.Services.AddSingleton(new OpenAIClient(openAiApiKey));

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseCors();

// Configuration values
var ankiConnectUrl = Environment.GetEnvironmentVariable("ANKI_CONNECT_URL") ?? "http://127.0.0.1:8765";
var deckName = Environment.GetEnvironmentVariable("DECK_NAME") ?? "Default";

// API endpoints
app.MapGet("/api/config", () => new 
{
    AnkiConnectUrl = ankiConnectUrl,
    DeckName = deckName
});

app.MapPost("/api/translate", async (TranslationRequest request, OpenAIClient openAiClient) =>
{
    if (string.IsNullOrWhiteSpace(request.DanishText))
    {
        return Results.BadRequest("Danish text is required");
    }

    try
    {
        var chatRequest = new ChatRequest(
            messages: new List<Message>
            {
                new Message(Role.System, 
                    "You are a helpful Danish to English translator. " +
                    "Respond ONLY with a JSON object with these fields: " +
                    "1. 'danish': the original Danish text, " +
                    "2. 'english': the English translation, " +
                    "3. 'pronunciation': Danish pronunciation guide (if relevant), " + 
                    "4. 'notes': any additional context, usage notes or grammar explanations. " +
                    "Format your response as valid JSON without additional commentary."),
                new Message(Role.User, request.DanishText)
            },
            model: "gpt-4o"
        );

        var response = await openAiClient.ChatEndpoint.GetCompletionAsync(chatRequest);
        var content = response.FirstChoice.Message.Content;

        // Try to parse the response as JSON
        try
        {
            using JsonDocument doc = JsonDocument.Parse(content);
            return Results.Ok(content);
        }
        catch (JsonException)
        {
            // If parsing fails, return the raw content
            return Results.Ok(new { danish = request.DanishText, english = content, pronunciation = "", notes = "" });
        }
    }
    catch (Exception ex)
    {
        return Results.Problem($"Error translating text: {ex.Message}");
    }
});

app.MapPost("/api/add-to-anki", async (AnkiCardRequest request) =>
{
    if (string.IsNullOrWhiteSpace(request.Danish) || string.IsNullOrWhiteSpace(request.English))
    {
        return Results.BadRequest("Both Danish and English text are required");
    }

    try
    {
        // Prepare notes field with pronunciation and notes if available
        var notesContent = new StringBuilder();
        
        if (!string.IsNullOrWhiteSpace(request.Pronunciation))
        {
            notesContent.AppendLine($"Pronunciation: {request.Pronunciation}");
        }
        
        if (!string.IsNullOrWhiteSpace(request.Notes))
        {
            notesContent.AppendLine(request.Notes);
        }

        // Create AnkiConnect request
        var ankiRequest = new
        {
            action = "addNote",
            version = 6,
            @params = new
            {
                note = new
                {
                    deckName = deckName,
                    modelName = "Basic",
                    fields = new
                    {
                        Front = request.Danish,
                        Back = $"{request.English}{(notesContent.Length > 0 ? $"<hr>{notesContent}" : "")}"
                    },
                    tags = new[] { "anki-helper" }
                }
            }
        };

        // Send request to AnkiConnect
        using var httpClient = new HttpClient();
        var content = new StringContent(
            JsonSerializer.Serialize(ankiRequest),
            Encoding.UTF8,
            "application/json");
        
        var response = await httpClient.PostAsync(ankiConnectUrl, content);
        var responseString = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            return Results.Problem($"AnkiConnect error: {responseString}");
        }

        return Results.Ok(new { message = "Card added successfully", ankiResponse = responseString });
    }
    catch (Exception ex)
    {
        return Results.Problem($"Error adding card to Anki: {ex.Message}");
    }
});

// Default route for SPA
app.MapFallbackToFile("index.html");

app.Run();

// Data models
public record TranslationRequest(string DanishText);

public record AnkiCardRequest
{
    [JsonPropertyName("danish")]
    public string Danish { get; set; } = "";
    
    [JsonPropertyName("english")]
    public string English { get; set; } = "";
    
    [JsonPropertyName("pronunciation")]
    public string Pronunciation { get; set; } = "";
    
    [JsonPropertyName("notes")]
    public string Notes { get; set; } = "";
}