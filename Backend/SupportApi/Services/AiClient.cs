using System.Data;
using System.Text;
using System.Text.Json;

namespace SupportApi.Services;

public class AiClient
{
    private const string SciBoxApiUrl = "https://llm.t1v.scibox.tech";
    private const string DefaultSciBoxModel = "Qwen2.5-72B-Instruct-AWQ";
    
    private const string MistralApiUrl = "https://api.mistral.ai";
    private const string DefaultMistralModel = "mistral-small-latest";
    private const string DefaultMistralEmbed = "mistral-embed";
    
    private const double Temperature = 0.0;
    private const double TopProbability = 1.0;
    private const int MaxTokens = 1000;

    private string _apiBaseUrl;
    public string ModelName { get; init; }
    public string EmbedModelName { get; init; }
    
    public bool IsMistral { get; init; }

    private readonly HttpClient _httpClient = new(new HttpClientHandler
    {
        AllowAutoRedirect = false
    });

    public AiClient(string apiKey)
    {
        if (apiKey.StartsWith("sk-"))
        {
            IsMistral = false;
            _apiBaseUrl = SciBoxApiUrl;
            ModelName = DefaultSciBoxModel;
            EmbedModelName = DefaultSciBoxModel;
        }
        else
        {
            _apiBaseUrl = MistralApiUrl;
            ModelName = DefaultMistralModel;
            EmbedModelName = DefaultMistralEmbed;
            IsMistral = true;
        }
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
    
    
    public async Task<string> GetEmbeddingAsync(string userInput)
    {
        var payload = new
        {
            model =  EmbedModelName,
            input =  userInput
        };

        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync($"{_apiBaseUrl}/v1/embeddings ", content);
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
        var response = await _httpClient.GetAsync($"{_apiBaseUrl}/models");
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

        var response = await _httpClient.PostAsync($"{_apiBaseUrl}/v1/chat/completions", content);
        response.EnsureSuccessStatusCode();
        
        var responseStream = await response.Content.ReadAsStreamAsync();
        return await JsonDocument.ParseAsync(responseStream);
    }
    
    public async Task<string> Ask(
        string systemPrompt,
        string userPrompt,
        string? model = null,
        double temperature = Temperature,
        double topP = TopProbability,
        int maxTokens = MaxTokens)
    {
        if (model is null) model = ModelName;
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
