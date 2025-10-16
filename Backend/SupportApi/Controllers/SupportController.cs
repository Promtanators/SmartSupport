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

        var token = "sk-5XAevFJ4WZEK-jZY4-ci5A";
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
    [HttpPost("template")]
    public async Task<IActionResult> SaveTemplate([FromBody] TemplateDto dto)
    {
        var listCategory = _db.BankFaqs 
            .Select(a => a.MainCategory)
            .Distinct()
            .ToList();
        var gen = new RecommendationsGenerator(_db.BankFaqs, _sciBoxClient);
        var mainCategory = await gen.GetMainCategoryAsync(listCategory, dto.Question);
        if (mainCategory == null)
        {
            return StatusCode(500);
        }
        var save = new SaveNewItemToDb(_db.BankFaqs, dto.Answer, dto.Question, mainCategory, _sciBoxClient);
        await save.AnalysisAndSave();
        await _db.SaveChangesAsync();
        return await Task.FromResult(Ok(save));
    }
}
