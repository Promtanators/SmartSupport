using System.Text.Json;
using SupportApi.Data;
using SupportApi.Models.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.Recommendations;
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
        var gen = new RecommendationsGenerator(_db.BankFaqs, _sciBoxClient);
        var recommendations = await gen.GetRecommendations(dto.Message);
        var response = new ResponseDto(recommendations);
        
        return await Task.FromResult(Ok(response));
    }

    [HttpGet("all")]
    public async Task<IActionResult> GetAll()
    {
        var faqs = _db.BankFaqs.ToList();
        var gen = new RecommendationsGenerator(_db.BankFaqs, _sciBoxClient);
        
        
        string message = "Карта заблокирована - что делать??";
        
        try
        {
            var startWaiting = DateTime.UtcNow;
            var recommendations = await gen.GetRecommendations(message);
            Console.WriteLine($"Общее время: {(DateTime.UtcNow - startWaiting).Seconds}c");
            return Ok(recommendations);
        }
        catch (Exception e)
        {
            return Ok($"Ошибка при получении ответов от LLM: {e.Message}");
        }
    }
}
