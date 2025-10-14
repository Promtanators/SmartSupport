using System.Text;
using System.Text.Json;

namespace SupportApi;

public class SciBoxClient
{
    private const string ApiBaseUrl = "https://llm.t1v.scibox.tech";
    private const string ModelNameQwen = "Qwen2.5-72B-Instruct-AWQ";
    private const string ModelNameBge = "bge-m3";

    private readonly HttpClient _httpClient = new(new HttpClientHandler
    {
        AllowAutoRedirect = false
    });

    public SciBoxClient(string apiKey)
    {
        _httpClient.DefaultRequestHeaders.Accept.Clear();
        _httpClient.DefaultRequestHeaders.Add("x-litellm-api-key", apiKey);
    }

    private string FormatJson(string responseString)
    {
        var doc = JsonDocument.Parse(responseString);
        return JsonSerializer.Serialize(doc, new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });
    }

    public async Task<string> GetModels()
    {
        var response = await _httpClient.GetAsync($"{ApiBaseUrl}/models");
        response.EnsureSuccessStatusCode();
        
        string responseString = response.Content.ReadAsStringAsync().Result;
        return FormatJson(responseString);
        
    }

    public async Task<string> ChatCompletion(
        string systemPrompt, 
        string userMessage,
        double temp,
        double topP,
        int maxTokens
            )
    {
        var payload = new
        {
            model = ModelNameQwen,
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
        
        var responseString = await response.Content.ReadAsStringAsync();
        return FormatJson(responseString);
    }

}