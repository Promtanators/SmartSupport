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

        var entities = await _GetEntities(categoryTree, message);
        return await _GetAnswers(entities, message);
    }

    public async Task<(string? MainCategory, string? SubCategory, string? TargetAudience)> _GetEntities(
        Dictionary<string, List<string>> categoryTree,
        string message)
    {
        var targetAudiences = await _bankFaqs
            .Select(b => b.TargetAudience)
            .Distinct()
            .ToListAsync();
        
        var mainCategories = categoryTree.Keys.ToList();
        var subCategories = categoryTree.Values.SelectMany(subs => subs).Distinct().ToList();

        var systemPrompt = _BuildEntitiesPrompt(categoryTree, targetAudiences, message);
        
        var startWaiting = DateTime.UtcNow;
        Logger.Log($"Запрос сущностей: {systemPrompt}");
        var answer = await _sciBoxClient.Ask(systemPrompt, message);
        Logger.Log($"ОТвет: {answer}");
        Logger.LogInformation($"Шаг 1: Сущности извлечены за {(DateTime.UtcNow - startWaiting).Seconds}c");
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

            Logger.LogInformation($"Сущности: [{mainCategory}, {subCategory}, {targetCategory}]");

            if (string.IsNullOrWhiteSpace(mainCategory) &&
                string.IsNullOrWhiteSpace(subCategory) &&
                string.IsNullOrWhiteSpace(targetCategory))
                throw new DataException($"No recommendations found for `{message}`");

            return (mainCategory, subCategory, targetCategory);
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
        Dictionary<string, List<string>> categoryTree,
        List<string> targetAudiences,
        string message)
    {
        var sb = new System.Text.StringBuilder();

        sb.AppendLine($"Выбери для каждой сущности индекс подходящего значения для вопроса:\n`{message}` ");
        sb.AppendLine("и верни строго JSON-массив целых чисел");
        sb.AppendLine($"в формате [{MainCategoryLabel}Index , {SubCategoryLabel}Index, {TargetAudienceLabel}Index]");
        sb.AppendLine("Индексы начинаются с 0. Если для сущности нет подходящего значения — используй -1.");
        sb.AppendLine("Подкатегорию можно выбирать только из подкатегорий выбранной главной категории.");
        sb.AppendLine("Ничего кроме JSON-массива не возвращай. Пример: [0,2,-1]");
        sb.AppendLine();

        var mainCategories = categoryTree.Keys.ToList();
        
        var jsonObj = new
        {
            MainCategory = mainCategories.Select((cat, i) => new { Index = i, Name = cat }).ToList(),
            SubCategory = mainCategories.Select((cat, i) => new
            {
                MainIndex = i,
                Subs = categoryTree[cat].Select((sub, j) => new { Index = j, Name = sub }).ToList()
            }).ToList(),
            TargetAudience = targetAudiences.Select((ta, i) => new { Index = i, Name = ta }).ToList()
        };
        
        return sb.ToString() + JsonSerializer.Serialize(
            jsonObj,
            new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            }
        );
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