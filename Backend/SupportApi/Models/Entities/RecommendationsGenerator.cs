using System.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using SupportApi.Data;
using SupportApi.Models.Dto;
using SupportApi.Models.Entities;

namespace SupportApi.Models.Entities;

public class RecommendationsGenerator
{
    private DbSet<BankFaq> _bankFaqs;
    private SciBoxClient _sciBoxClient;

    private const string MainCategoryLabel = "MainCategory";
    private const string SubCategoryLabel = "SubCategory";
    private const string TargetAudienceLabel = "TargetAudience";
    private const int MaxTemplates = 10;

    public RecommendationsGenerator(DbSet<BankFaq> bankFaqs, SciBoxClient sciBoxClient)
    {
        _bankFaqs = bankFaqs;
        _sciBoxClient = sciBoxClient;
    }

    public async Task<List<AnswerScoreDto>> GetRecommendationsFast(string message)
    {
        string userEmbedding = await _sciBoxClient.GetEmbeddingAsync(message);
        double[] embeddingResult = JsonSerializer.Deserialize<double[]>(userEmbedding) 
                                   ?? throw new NullReferenceException($"{nameof(embeddingResult)} is null");
        
        
        List<(string TemplateResponse, double matchIndex)> embeddingValues = new();
        
        foreach (var bankFaq in _bankFaqs)
        {
            var embedding = JsonSerializer.Deserialize<double[]>(bankFaq.ExampleEmbedding) 
                            ?? throw new NullReferenceException($"{nameof(embeddingResult)} is null");;
            
            double match = MathOperations.CosineSimilarity(embeddingResult, embedding);
            
            embeddingValues.Add((bankFaq.TemplateResponse, match));
        }
        
        var matchList = embeddingValues
            .OrderByDescending(x => x.Item2)
            .Take(MaxTemplates)
            .ToList();
        
        return matchList
            .Select(x => new AnswerScoreDto(x.TemplateResponse, (int)(x.matchIndex * 100)))
            .ToList();
    }
    
    public async Task<List<AnswerScoreDto>> GetRecommendations(string message)
    {
        var categoryTree = await _bankFaqs
            .GroupBy(b => b.MainCategory)
            .ToDictionaryAsync(
                g => g.Key,
                g => g.Select(b => b.Subcategory)
                    .Distinct()
                    .ToList()
            );

        var mainCategoryTask = _GetMainCategoryAsync(categoryTree, message);
        var messageEmbTask = _sciBoxClient.GetEmbeddingAsync(message);

        await Task.WhenAll(mainCategoryTask, messageEmbTask);
        
        var mainCategory = mainCategoryTask.Result;
        var messageEmb = JsonSerializer.Deserialize<double[]>(messageEmbTask.Result);

        if (messageEmb is null) throw new DataException("Cant get embeddings for message");
        if (mainCategory is null) throw new DataException($"Cant get {MainCategoryLabel} for message");
        
        var bankFaqs = await _bankFaqs
            .Where(b => b.MainCategory == mainCategory)
            .ToListAsync();

        var ratedAnswers = _RateByEmbedding(bankFaqs, messageEmb);
        return await _GetAnswersAsync(ratedAnswers, message);
    }
    
    private List<(string answer, int score)> _RateByEmbedding(
        List<BankFaq> bankFaqs,
        double[] messageEmb)
    {
        List<(string answer, int score)> ratedAnswers = new();
        
        foreach (var bankFaq in bankFaqs)
        {
            var embedding = JsonSerializer.Deserialize<double[]>(bankFaq.ExampleEmbedding);
            if (embedding is null)
                throw new JsonException($"Cant deserialize embedding for example `{bankFaq.ExampleQuestion}");
            var score = MathOperations.CosineSimilarity(embedding, messageEmb);
            ratedAnswers.Add((answer: bankFaq.TemplateResponse, (int)(score*100)));
        }

        return ratedAnswers
            .OrderByDescending(a => a.score)
            .Distinct()
            .Take(MaxTemplates)
            .ToList();
    } 
    

    public async Task<string?> _GetMainCategoryAsync(
        Dictionary<string, List<string>> categoryTree,
        string message)
    {
        var mainCategories = categoryTree.Keys.ToList();
        var systemPrompt = _BuildMainCategoryPrompt(mainCategories);

        var startWaiting = DateTime.UtcNow;
        var answer = await _sciBoxClient.Ask(systemPrompt, message);
        Logger.LogInformation($"Шаг 1: Сущности извлечены за {(DateTime.UtcNow - startWaiting).Seconds}c");
        try
        {
            var index = int.Parse(answer);
            if (index < 0) return null;
            var mainCategory = mainCategories[index];
            
            Logger.Log($"Главная категория: {mainCategory}");
            return mainCategory;
        }
        catch (Exception ex)
        {
            throw new DataException("Model returned bad json categories: " + answer + ex);
        }
    }

    private async Task<List<AnswerScoreDto>> _GetAnswersAsync(
        List<(string answer, int score)> ratedAnswers,
        string message)
    {
        var systemPrompt = _BuildAnswersPrompt(ratedAnswers);
        var response = await _sciBoxClient.Ask(systemPrompt, message);
        
        var indexes = JsonSerializer.Deserialize<List<int>>(response);
        if(indexes is null || indexes.Count == 0) throw new DataException("Cant get answers for message");
        Logger.LogJson("indexes", indexes);
        
        var chosenAnswers = new List<AnswerScoreDto>();
        foreach (var index in indexes)
        {
            chosenAnswers.Add(new AnswerScoreDto(ratedAnswers[index].answer, ratedAnswers[index].score));
        }
        return chosenAnswers;
    }

    private string _BuildMainCategoryPrompt(
        List<string> mainCategories)
    {
        var sb = new System.Text.StringBuilder();

        sb.AppendLine("Для любого запроса пользователя выдели основную категорию из массива");
        sb.AppendLine($"[{string.Join(",", mainCategories)}]");
        sb.AppendLine("В ответ пришли строго одно число - индекс категории из массива от нуля");
        sb.AppendLine("если подходящей категории нет, верни -1");

        return sb.ToString();
    }
    
    
    private string _BuildAnswersPrompt(List<(string answer, int score)> ratedAnswers)
    {
        var sb = new System.Text.StringBuilder();
        sb.Append("Для запроса пользователя выбирай подходящие ответы и верни строго JSON-массив подходящих индексов.\n");
        sb.Append("Ни слов, ни пояснений, ничего кроме JSON-массива. Пример: [0,2,3] или [] или [2]\n\n");
        sb.Append("Варианты (не менять):\n");
        
        int index = 0;
        foreach (var answer in ratedAnswers)
        {
            var text = string.IsNullOrWhiteSpace(answer.answer) ? "-" : answer.answer.Trim();
            sb.Append(index).Append(": ").Append(text).Append('\n');
            index++;
        }
        return sb.ToString();
    }
}