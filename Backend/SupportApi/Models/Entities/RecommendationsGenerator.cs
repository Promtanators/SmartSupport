using Microsoft.EntityFrameworkCore;
using SupportApi.Data;

namespace SupportApi.Models.Entities;

public class RecommendationsGenerator
{
    private DbSet<BankFaq> _bankFaqs;
    public RecommendationsGenerator(DbSet<BankFaq> bankFaqs)
    {
        _bankFaqs = bankFaqs;
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
        var token = Environment.GetEnvironmentVariable("SCIBOX_API_KEY") 
                    ?? throw new InvalidOperationException("Environment variable SCIBOX_API_KEY is not set.");
        var client = new SciBoxClient(token);
        var answer = await client.Ask("Ты выбираешь на каждое сообщение пользователя 1 ключевое слово" +
                                      " из каждого списка, если ключевое слово не найдено, то напиши " +
                                      "-. Ответ оформи в json формат, пример: {MainCategory: что-то из первого списка, Subcategory:" +
                                      " что-то из второго списка, TargetAudience: что-то из третьего списка}. Вот списки: "
                                      + mainCategoriesString + "|" + subCategoriesString + "|" + targetAudiencesString, message);
        Console.WriteLine(answer);
        return new ();
    }
}