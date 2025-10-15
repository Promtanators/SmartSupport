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

        var mainCategory = await _GetEntities(categoryTree, message);

        var bankFaqs = await _bankFaqs
            .Where(b => b.MainCategory == mainCategory)
            .ToListAsync();


    }
    
    private async Task<Dictionary<string,double>> _FilterByEmbedding(List<BankFaq> bankFaqs, string message)
    {
        Dictionary<string, double> embeddingValues = new();
        
        foreach (var bankFaq in bankFaqs)
        {
            var embedding = JsonSerializer.Deserialize<double[]>(bankFaq.ExampleEmbedding);
            MathOperations.CosineSimilarity();
        }

        return embeddingValues;
    } 
    
    
    
    

    public async Task<string?> _GetEntities(
        Dictionary<string, List<string>> categoryTree,
        string message)
    {
        var mainCategories = categoryTree.Keys.ToList();
        var systemPrompt = _BuildEntitiesPrompt(mainCategories, message);

        var startWaiting = DateTime.UtcNow;
        Logger.Log($"Запрос сущностей: {systemPrompt}");
        var answer = await _sciBoxClient.Ask(systemPrompt, message);
        Logger.Log($"ОТвет: {answer}");
        Logger.LogInformation($"Шаг 1: Сущности извлечены за {(DateTime.UtcNow - startWaiting).Seconds}c");
        try
        {
            var index = int.Parse(answer);
            if (index < 0) return null;
            
            return mainCategories[index];
        }
        catch (Exception ex)
        {
            throw new DataException("Model returned bad json categories: " + answer + ex);
        }
    }

    private async Task<List<AnswerScoreDto>> _GetAnswers(
        (string? MainCategory, string? SubCategory, string? TargetAudience) keys,
        string message)
    {
        var rightAnswers = await _bankFaqs
            .Where(b => keys.MainCategory == null || b.MainCategory == keys.MainCategory)
            .Where(b => keys.SubCategory == null || b.Subcategory == keys.SubCategory)
            .Where(b => keys.TargetAudience == null || b.TargetAudience == keys.TargetAudience)
            .Select(b => b.TemplateResponse)
            .Distinct()
            .OrderBy(t => t)
            .Take(MaxTemplates)
            .ToListAsync();
        // foreach (var b in _bankFaqs)
        // {
        //     Logger.LogInformation($"[DB] {b.MainCategory} | {b.Subcategory} | {b.TargetAudience}");
        // }
        if(rightAnswers.Count == 0) throw new DataException("No recommendations found for `" + message + "`");
        Logger.LogJson("rightAnswers", rightAnswers);

        var startWaiting = DateTime.UtcNow;
        var response = await _sciBoxClient.Ask(_BuildAnswersPrompt(rightAnswers), message);
        Logger.LogInformation($"Шаг 2: Рекомендации по сущностям за {(DateTime.UtcNow - startWaiting).Seconds}c");
        
        List<List<int>>? lists;
        try
        {
            lists = JsonSerializer.Deserialize<List<List<int>>>(response);
        }
        catch (Exception ex)
        {
            throw new JsonException("Model returned bad json: " + response + ex);
        }

        if (lists is null || lists.Count != 2)
            throw new DataException("Model returned bad json lists: " + response);

        var indices = lists[0];
        var scores = lists[1];

        if (indices.Count != scores.Count)
            throw new DataException("Indices and scores counts do not match");

        var recommendations = indices
            .Select((idx, i) => new AnswerScoreDto(rightAnswers[idx], scores[i]))
            .ToList();

        Logger.LogJson("Ответы", recommendations);
        return recommendations;
    }

    private string _BuildEntitiesPrompt(
        List<string> mainCategories,
        string message)
    {
        var sb = new System.Text.StringBuilder();

        sb.AppendLine($"Для вопроса `{message}` выдели основную категорию из массива");
        sb.AppendLine($"[{string.Join(",", mainCategories)}]");
        sb.AppendLine("В ответ пришли строго одно число - индекс категории из массива от нуля");
        sb.AppendLine("если подходящей категории нет, верни -1");

        return sb.ToString();
    }



    
    private string _BuildAnswersPrompt(IEnumerable<string> answers)
    {
        var sb = new System.Text.StringBuilder();
        sb.Append("Выбери подходящие ответы и верни строго JSON-массив из двух массивов: [indices, scores]. ");
        sb.Append("Первый элемент — массив индексов выбранных ответов (начиная с 0). ");
        sb.Append("Второй элемент — массив соответствующих процентов пригодности (целые 0-100). ");
        sb.Append("Ни слов, ни пояснений, ничего кроме JSON-массива. Пример: [[0,2,3],[90,70,50]]\n\n");
        sb.Append("Варианты (не менять):\n");
        
        int index = 0;
        foreach (var answer in answers)
        {
            var text = string.IsNullOrWhiteSpace(answer) ? "-" : answer.Trim();
            sb.Append(index).Append(": ").Append(text).Append('\n');
            index++;
        }
        return sb.ToString();
    }




}