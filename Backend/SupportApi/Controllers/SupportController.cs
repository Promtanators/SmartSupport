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
        //var recommendations = await gen.GetRecommendations(dto.Message);
        var recommendations = new List<AnswerScoreDto>
{
    new AnswerScoreDto(
        "Стать клиентом ВТБ (Беларусь) можно онлайн через сайт vtb.by или мобильное приложение VTB mBank. Для регистрации потребуются паспорт и номер телефона. После регистрации через МСИ (Межбанковскую систему идентификации) вы получите доступ к банковским услугам. .",
        85),
    new AnswerScoreDto(
        "МСИ позволяет пройти идентификацию онлайн, используя данные других банков, где вы уже являетесь клиентом. Это упрощает процедуру регистрации и делает её быстрой и безопасной..",
        97),
    new AnswerScoreDto(
        "Для регистрации в качестве нового клиента необходим паспорт гражданина Республики Беларусь и контактный номер мобильного телефона для получения SMS-подтверждений. ",
        55),
    new AnswerScoreDto(
        "Стать клиентом ВТБ (Беларусь) можно онлайн через сайт vtb.by или мобильное приложение VTB mBank. Для регистрации потребуются паспорт и номер телефона. После регистрации через МСИ (Межбанковскую систему идентификации) вы получите доступ к банковским услугам. .",
        15),
    new AnswerScoreDto(
        "После регистрации вы получите логин и пароль для входа в систему Интернет-банк. При первом входе рекомендуется изменить временный пароль на постоянный и настроить дополнительные параметры безопасности. ",
        78),
    new AnswerScoreDto(
        "Мобильное приложение VTB mBank можно скачать в App Store для iOS или Google Play для Android. После установки войдите с логином и паролем от Интернет-банка и пройдите первоначальную настройку.",
        90),
    new AnswerScoreDto(
        "Если не получается войти в Интернет-банк, проверьте правильность ввода логина и пароля. При забытом пароле воспользуйтесь функцией восстановления. Если проблема не решается, обратитесь в контакт-центр по номеру 250 или +375 (17/29/33) 309 15 15. Вы можете получить онлайн-консультацию (в текстовом формате) в будние дни с 9:00 до 17:30, написав специалисту банка в Telegram либо в чат на сайте (ссылки на сайте банка).",
        77)
};

        var response = new ResponseDto(recommendations);
        
        return await Task.FromResult(Ok(response));
    }

    [HttpGet("all")]
    public async Task<IActionResult> GetAll()
    {
        var faqs = _db.BankFaqs.ToList();
        var gen = new RecommendationsGenerator(_db.BankFaqs, _sciBoxClient);
        
        
        string message = "Карта сломалась пополам - что делать??";
        
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
