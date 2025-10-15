using System.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SupportApi.Data;
using SupportApi.Models.Dto;

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
        
        var targetAudiences = await _bankFaqs
            .Select(b => b.TargetAudience)
            .Distinct()
            .ToListAsync();
        
        var mainCategories = categoryTree.Keys.ToList();
        var subCategories = categoryTree.Values.SelectMany(subs => subs).Distinct().ToList();

        var systemPrompt = _BuildRecommendationPrompt(categoryTree, targetAudiences);

        var startWaiting = DateTime.UtcNow;
        var answer = await _sciBoxClient.Ask(systemPrompt, message);

        try
        {
            var indexes = JsonSerializer.Deserialize<List<int>>(answer);
            if (indexes == null || indexes.Count < 3)
                throw new DataException("Model returned invalid JSON: " + answer);

            int mainIndex = indexes[0];
            int subIndex = indexes[1];
            int targetIndex = indexes[2];

            if (mainIndex < -1 || mainIndex >= mainCategories.Count)
                throw new DataException($"{MainCategoryLabel} index out of range: {mainIndex}");
            if (subIndex < -1 || subIndex >= subCategories.Count)
                throw new DataException($"{SubCategoryLabel} index out of range: {subIndex}");
            if (targetIndex < -1 || targetIndex >= targetAudiences.Count)
                throw new DataException($"{TargetAudienceLabel} index out of range: {targetIndex}");

            string? mainCategory = mainIndex >= 0 ? mainCategories[mainIndex] : null;
            string? subCategory = subIndex >= 0 ? subCategories[subIndex] : null;
            string? targetCategory = targetIndex >= 0 ? targetAudiences[targetIndex] : null;

            Console.WriteLine($"Сущности: [{mainCategory}, {subCategory}, {targetCategory}]");

            return await _GetAnswers((mainCategory, subCategory, targetCategory), message);
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
            .Where(b => keys.TargetAudience == null || b.TargetAudience == keys.TargetAudience)
            .Select(b => b.TemplateResponse)
            .Distinct()
            .OrderBy(t => t)
            .Take(MaxTemplates)
            .ToListAsync();
        
        if(rightAnswers.Count == 0) throw new DataException("No recommendations found for `" + message + "`");
        Console.WriteLine($"rightAnswers: {JsonSerializer.Serialize(
            rightAnswers,
            new JsonSerializerOptions { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping }
        )}");

        var startWaiting = DateTime.UtcNow;
        var response = await _sciBoxClient.Ask(_BuildAnswersPrompt(rightAnswers), message);
        
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

        Console.WriteLine($"Ответы: {JsonSerializer.Serialize(
            recommendations,
            new JsonSerializerOptions { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping }
        )}");
        return recommendations;
    }

    private string _BuildRecommendationPrompt(
        Dictionary<string, List<string>> categoryTree,
        List<string> targetAudiences)
    {
        var mainCategories = categoryTree.Keys.ToList();
        var sb = new System.Text.StringBuilder();
        sb.Append($"Выбери для каждой сущности индекс подходящего значения и верни строго JSON-массив целых чисел в формате [MainCategoryIndex , SubCategoryIndex, TargetAudienceIndex]. ");
        sb.Append("Индексы начинаются с 0. Если для сущности нет подходящего значения — используй -1. Подкатегорию можно выбирать только из списка подкатегорий выбранной главной категории. Ничего кроме JSON-массива не возвращай. Пример: [0,2,-1]\n\n");

        sb.Append($"{MainCategoryLabel}:\n");
        for (int i = 0; i < mainCategories.Count; i++)
        {
            sb.Append(i).Append(": ").Append(mainCategories[i]).Append('\n');
        }

        sb.Append($"\n{SubCategoryLabel}:\n");
        for (int i = 0; i < mainCategories.Count; i++)
        {
            var mainCat = mainCategories[i];
            sb.Append(mainCat).Append(":\n");
            var subs = categoryTree[mainCat];
            for (int j = 0; j < subs.Count; j++)
            {
                sb.Append("  ").Append(j).Append(": ").Append(subs[j]).Append('\n');
            }
        }

        sb.Append($"\n{TargetAudienceLabel}:\n");
        for (int i = 0; i < targetAudiences.Count; i++)
        {
            sb.Append(i).Append(": ").Append(targetAudiences[i]).Append('\n');
        }

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