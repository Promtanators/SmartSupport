using System.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SupportApi.Data;

namespace SupportApi.Models.Entities;

public class RecommendationsGenerator
{
    
    private readonly string SystemPromptRecommendation = "Ты выбираешь на каждое сообщение пользователя 1 ключевое слово" +
                                                         " из каждого списка, если ключевое слово не найдено, то напиши " +
                                                         "-. Ответ оформи в json формат, пример: {MainCategory: что-то из первого списка, SubCategory:" +
                                                         " что-то из второго списка, TargetAudience: что-то из третьего списка}. Вот списки: ";
    
    private readonly string SystemPromptAnswers = "Выбери самый подходящий ответ или ответы, который отвечает на вопрос пользователя" +
                                                  "Ответ оформи просто в столбец друг за другом." +
                                                  "(Если не нашёл ни одного ответа то не придумывай, а выводи: Ответ не найден в базе знаний). Вот список:";
    
    private DbSet<BankFaq> _bankFaqs;
    private SciBoxClient _sciBoxClient;
    public RecommendationsGenerator(DbSet<BankFaq> bankFaqs)
    {
        _bankFaqs = bankFaqs;
        
        var token = Environment.GetEnvironmentVariable("SCIBOX_API_KEY")
                    ?? throw new InvalidOperationException("Environment variable SCIBOX_API_KEY is not set.");
        _sciBoxClient = new SciBoxClient(token);
    }

    public async Task<List<string>> GetRecommendations (string message)
    {
        var mainCategories = _bankFaqs
            .Select(b => b.MainCategory)
            .Distinct()
            .ToList();
        var subcategories = _bankFaqs
            .Select(b => b.Subcategory)
            .Distinct()
            .ToList();
        var targetAudiences = _bankFaqs
            .Select(b => b.TargetAudience)
            .Distinct()
            .ToList();
        
        string mainCategoriesString = string.Join(",",  mainCategories);
        string subCategoriesString = string.Join(",",  subcategories);
        string targetAudiencesString = string.Join(",", targetAudiences);
        string systemPrompt = SystemPromptRecommendation + mainCategoriesString + "|" + subCategoriesString + "|" +
                              targetAudiencesString;
        
        var answer = await _sciBoxClient.Ask(systemPrompt, message);
        
        try
        {
            var jsonAnswer = JsonDocument.Parse(answer);

            var listRecommendations = new List<string?>();
            listRecommendations.Add(jsonAnswer.RootElement.GetProperty("MainCategory").GetString());
            listRecommendations.Add(jsonAnswer.RootElement.GetProperty("SubCategory").GetString());
            listRecommendations.Add(jsonAnswer.RootElement.GetProperty("TargetAudience").GetString());

            if (listRecommendations.Any(x => string.IsNullOrWhiteSpace(x) || x == "-".Trim()))
            {
                throw new DataException("Ответ не найден в базе знаний");
            }

            return listRecommendations;
        }
        catch
        {
            throw new DataException("Языковая модель вернула невалидный JSON");
        }
    }

    public async Task<List<string>> GetAnswers(List<string> recommendations, string message)
    {
        List<Func<BankFaq, bool>> filters = new()
        {
            b => b.MainCategory == recommendations[0] && b.Subcategory == recommendations[1],
            b => b.MainCategory == recommendations[0]
        };


        foreach (var filter in filters)
        {
                var rightAnswers = _bankFaqs.Where(filter).Select(a => a.TemplateResponse).ToList();

                string listAnswer = string.Join(",", rightAnswers);
                var systemPrompt = SystemPromptAnswers + listAnswer;
            
                var response = await _sciBoxClient.Ask(systemPrompt, message);

                List<string> answers = response.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries).ToList();

                if (answers.Count != 0 && !answers.Any(a => a.Contains("Ответ не найден в базе знаний"))) return answers;
        }
        
        throw new DataException("Ответ не найден в базе знаний");
    }
}