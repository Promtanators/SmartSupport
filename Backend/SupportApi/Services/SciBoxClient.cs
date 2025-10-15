using System.Data;
using System.Text;
using System.Text.Json;

namespace SupportApi;

public class SciBoxClient
{
    private const string ApiBaseUrl = "https://llm.t1v.scibox.tech";
    private const string ModelNameQwen = "Qwen2.5-72B-Instruct-AWQ";
    private const string OpenRouterModelName = "qwen/qwen3-vl-8b-thinking";
    private const string ModelNameBge = "bge-m3";
    
    private const double Temperature = 0.1;
    private const double TopProbability = 0.9;
    private const int MaxTokens = 1000;

    private readonly HttpClient _httpClient = new(new HttpClientHandler
    {
        AllowAutoRedirect = false
    });

    public SciBoxClient(string apiKey)
    {
        _httpClient.DefaultRequestHeaders.Accept.Clear();
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
    }

    private string _FormatJson(string responseString)
    {
        var doc = JsonDocument.Parse(responseString);
        return JsonSerializer.Serialize(doc, new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });
    }
    
    
    public async Task<string> GetEmbedding(
        string userInput
    )
    {
        var payload = new
        {
            model =  ModelNameBge,
            input =  userInput
        };

        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync($"{ApiBaseUrl}/v1/embeddings ", content);
        response.EnsureSuccessStatusCode();
        
        var responseString = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(responseString);

        var embedding = doc.RootElement
            .GetProperty("data")[0]
            .GetProperty("embedding")
            .ToString();
        
        return embedding;
    }
    public async Task<string> GetModels()
    {
        var response = await _httpClient.GetAsync($"{ApiBaseUrl}/models");
        response.EnsureSuccessStatusCode();
        
        string responseString = response.Content.ReadAsStringAsync().Result;
        return _FormatJson(responseString);
        
    }

    public async Task<JsonDocument> ChatCompletion(
        string systemPrompt, 
        string userMessage,
        string model,
        double temp,
        double topP,
        int maxTokens
            )
    {
        var payload = new
        {
            model = model,
            messages = new[]
            {
                new { role = "system", content = $"{systemPrompt}" },
                new { role = "user", content = $"{userMessage}" }
            },
            temperature = temp,
            top_p = topP,
            max_tokens = maxTokens
        };

        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync($"{ApiBaseUrl}/v1/chat/completions", content);
        response.EnsureSuccessStatusCode();
        
        var responseStream = await response.Content.ReadAsStreamAsync();
        return await JsonDocument.ParseAsync(responseStream);
    }
    
    public async Task<string> Ask(
        string systemPrompt,
        string userPrompt,
        string model = ModelNameQwen,
        double temperature = Temperature,
        double topP = TopProbability,
        int maxTokens = MaxTokens)
    {
        var jsonDoc = await ChatCompletion(systemPrompt, userPrompt, model: model, temp: temperature, topP: topP, maxTokens: maxTokens);
        var content = jsonDoc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        if (content is null) throw new DataException("No answers found");
        return content;
    }
}
