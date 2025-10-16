using System.Text.Json;
using SupportApi.Data;
using SupportApi.Models.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.Recommendations;
using Microsoft.EntityFrameworkCore;
using SupportApi.Models.Entities;

namespace SupportApi.Controllers;

[ApiController]
[Route("api/v1")]
public class SupportController : ControllerBase
{
    private readonly SupportDbContext _db;
    private readonly SciBoxClient _sciBoxClient;
    
    private const string ModelNameQwen = "Qwen2.5-72B-Instruct-AWQ";
    private const string ModelNameBge = "bge-m3";

    public SupportController(SupportDbContext db)
    {
        _db = db;
        
        var token = Environment.GetEnvironmentVariable("SCIBOX_API_KEY")
                    ?? Environment.GetEnvironmentVariable("key")
                    ?? throw new InvalidOperationException("Environment variable SCIBOX_API_KEY and key is not set.");
        _sciBoxClient = new SciBoxClient(token);
    }
    
    [HttpPost("ask")]
    public async Task<IActionResult> Ask([FromBody] AskDto dto)
    {
        ResponseDto? response = null;
        try
        {
            var gen = new RecommendationsGenerator(_db.BankFaqs, _sciBoxClient);
            var recommendations = await gen.GetRecommendations(dto.Message);

            response = new ResponseDto(recommendations);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "При обработке post ask");
            response = new ResponseDto(new());
        }

        return await Task.FromResult(Ok(response));
    }
    
    [HttpPost("fastask")]
    public async Task<IActionResult> FastAsk([FromBody] AskDto dto)
    {
        var gen = new RecommendationsGenerator(_db.BankFaqs, _sciBoxClient);
        var recommendations = await gen.GetRecommendationsFast(dto.Message);
        
        var response = new ResponseDto(recommendations);
        
        return await Task.FromResult(Ok(response));
    }
    
    [HttpGet("update")]
    public async Task<IActionResult> Update()
    {
        try
        {
            foreach (var dbBankFaq in _db.BankFaqs)
            {
                var question = dbBankFaq.ExampleQuestion;

                var embedding = await _sciBoxClient.GetEmbeddingAsync(question);

                dbBankFaq.ExampleEmbedding = embedding;
            }
        }
        catch (Exception ex)
        {
            return await Task.FromResult(Ok("Error while updating embedding column: " + ex.Message)); 
        }
        finally
        {
            await _db.SaveChangesAsync();
        }

        return await Task.FromResult(Ok("Database updated successfully"));
    }

    [HttpGet("all")]
    public async Task<IActionResult> GetAll()
    {
        var faqs = _db.BankFaqs.ToList();
        var gen = new RecommendationsGenerator(_db.BankFaqs, _sciBoxClient);


        string message = "Как стать клиентом банка?";

        try
        {
            var startWaiting = DateTime.UtcNow;
            var recommendations = await gen.GetRecommendations(message);
            Logger.LogInformation($"Общее время: {(DateTime.UtcNow - startWaiting).Seconds}c");
            return Ok(recommendations);
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Ошибка при получении ответов от LLM");
            return Ok($"Ошибка при получении ответов от LLM: {e.Message}");
        }
    }
}
