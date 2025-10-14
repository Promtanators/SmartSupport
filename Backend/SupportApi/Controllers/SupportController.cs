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
    
    private const string ModelNameQwen = "Qwen2.5-72B-Instruct-AWQ";
    private const string ModelNameBge = "bge-m3";

    public SupportController(SupportDbContext db)
    {
        _db = db;
    }
    
    [HttpPost("ask")]
    public async Task<IActionResult> Ask([FromBody] AskDto dto)
    {
        var gen = new RecommendationsGenerator(_db.BankFaqs);
        var recommendations = await gen.GetRecommendations(dto.Message);
        var response = new ResponseDto(recommendations);
        
        return await Task.FromResult(Ok(response));
    }

    [HttpGet("all")]
    public async Task<IActionResult> GetAll()
    {
        var faqs = _db.BankFaqs.ToList();
        var gen = new RecommendationsGenerator(_db.BankFaqs);
        var recommendations = await gen.GetRecommendations("Как стать клиентом банка онлайн?");
        return Ok(faqs);
    }
}
