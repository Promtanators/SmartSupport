using System.Text.Json;
using SupportApi.Data;
using SupportApi.Models.Dto;
using Microsoft.AspNetCore.Mvc;

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
        var response = new ResponseDto(["Подсказка 1", "Подсказка 2"]);
        return await Task.FromResult(Ok(response));
    }
}
