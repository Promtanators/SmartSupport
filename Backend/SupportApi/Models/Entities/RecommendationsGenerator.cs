using System.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using SupportApi.Data;
using SupportApi.Models.Dto;
using SupportApi.Models.Entities;
using SupportApi.Services;

namespace SupportApi.Models.Entities;

public class RecommendationsGenerator
{
    private DbSet<BankFaq> _bankFaqs;
    private AiClient _aiClient;

    private const string MainCategoryLabel = "MainCategory";
    private const string SubCategoryLabel = "SubCategory";
    private const string TargetAudienceLabel = "TargetAudience";
    private const int MaxTemplates = 10;
    private const int TrustScore = 85;

    public RecommendationsGenerator(DbSet<BankFaq> bankFaqs, AiClient aiClient)
    {
        _bankFaqs = bankFaqs;
        _aiClient = aiClient;
    }

    public async Task<List<AnswerScoreDto>> GetRecommendationsFast(string message)
    {
        string userEmbedding = await _aiClient.GetEmbeddingAsync(message);
        double[] embeddingResult = JsonSerializer.Deserialize<double[]>(userEmbedding) 
                                   ?? throw new NullReferenceException($"{nameof(embeddingResult)} is null");
        
        List<(string TemplateResponse, string MainCategory, string SubCategory, string TargetAudience, double matchIndex)> embeddingValues = new();
        
        foreach (var bankFaq in _bankFaqs)
        {
            string? jsonEmbedding = _aiClient.IsMistral? bankFaq.ExampleMistralEmbedding : bankFaq.ExampleSciBoxEmbedding;
            if (jsonEmbedding is null)
                throw new DataException($"There is no embedding for {_aiClient.EmbedModelName}");
            
            var embedding = JsonSerializer.Deserialize<double[]>(jsonEmbedding) 
                            ?? throw new NullReferenceException($"{nameof(embeddingResult)} is null");;
            double match = MathOperations.CosineSimilarity(embeddingResult, embedding);
            
            embeddingValues.Add((bankFaq.TemplateResponse, bankFaq.MainCategory, bankFaq.Subcategory, bankFaq.TargetAudience ,match));
        }
        
        var matchList = embeddingValues
            .OrderByDescending(x => x.matchIndex)
            .Take(MaxTemplates)
            .ToList();
        
        return matchList
            .Select(x => new AnswerScoreDto(x.TemplateResponse, (int)(x.matchIndex * 100) , x.MainCategory, x.SubCategory, x.TargetAudience))
            .ToList();
    }
    
    public async Task<List<AnswerScoreDto>> GetRecommendations(string message)
    {
        var mainCategories = await _bankFaqs
            .Select(b => b.MainCategory)
            .Distinct()
            .ToListAsync();
        
        var targetAudiences = await _bankFaqs
            .Select(b => b.TargetAudience)
            .Distinct()
            .ToListAsync();
        
        var messageEmbTask = _aiClient.GetEmbeddingAsync(message);
        var entitiesTask = GetEntitiesAsync(mainCategories, targetAudiences, message);
        await Task.WhenAll(messageEmbTask, entitiesTask);

        var messageEmbJson = await messageEmbTask;
        var (mainCategory, targetAudience) = await entitiesTask;
        
        var messageEmb = JsonSerializer.Deserialize<double[]>(messageEmbJson);
        if (messageEmb is null) throw new DataException("Cant get embeddings for message");
        
        var ratedAnswers = _RateByEmbedding(_bankFaqs, messageEmb);
        var trustedAnswers = ratedAnswers
            .Where(a => a.score >= TrustScore)
            .Select(a => new AnswerScoreDto(a.answer, a.score, mainCategory ?? "", "", targetAudience ?? ""))
            .ToList();
        
        if(trustedAnswers.Count > 0) return trustedAnswers;
        
        if (mainCategory is null) throw new DataException($"Cant get {MainCategoryLabel} for message");
        
        var filtered = await _bankFaqs
            .Where(b => b.MainCategory == mainCategory)
            .ToListAsync();
        ratedAnswers = _RateByEmbedding(filtered, messageEmb);
        
        
        // var verifiedAnswers = await _GetAnswersAsync(ratedAnswers, message);
        // return verifiedAnswers
        //     .UnionBy(trustedAnswers, a => a.Answer)
        //     .ToList();
        return await _GetAnswersAsync(ratedAnswers, message, mainCategory, targetAudience ?? "");
    }
    
    private List<(string answer, int score)> _RateByEmbedding(
        IEnumerable<BankFaq> bankFaqs,
        double[] messageEmb)
    {
        List<(string answer, int score)> ratedAnswers = new();
        
        foreach (var bankFaq in bankFaqs)
        {
            string? jsonEmbedding = _aiClient.IsMistral? bankFaq.ExampleMistralEmbedding : bankFaq.ExampleSciBoxEmbedding;
            if (jsonEmbedding is null)
                throw new DataException($"There is no embedding for model `{_aiClient.EmbedModelName}`, ExampleQuestion `{bankFaq.ExampleQuestion}`");
            
            var embedding = JsonSerializer.Deserialize<double[]>(jsonEmbedding);
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
    

    public async Task<(string? MainCategory, string? TargetAudience)> GetEntitiesAsync(
        List<string> mainCategories,
        List<string> targetAudiences,
        string message)
    {
        var systemPrompt = _BuildEntitiesPrompt(mainCategories, targetAudiences);

        var startWaiting = DateTime.UtcNow;
        var answer = await _aiClient.Ask(systemPrompt, message);
        Logger.LogInformation($"Шаг 1: Сущности извлечены за {(DateTime.UtcNow - startWaiting).Seconds}c");
        try
        {
            var indexes = JsonSerializer.Deserialize<List<int>>(answer);
            if(indexes == null || indexes.Count != 2) throw new DataException("Cant deserialize entities answer");
            var mainCategory = indexes[0] >= 0 ? mainCategories[indexes[0]] : null;
            var targetAudience = indexes[1] >= 0 ? targetAudiences[indexes[1]] : null;
            
            Logger.Log($"Главная категория: {mainCategory}, Целевая аудитория: {targetAudience}");
            return (mainCategory, targetAudience);
        }
        catch (Exception ex)
        {
            throw new DataException("Model returned bad json categories: " + answer + ex);
        }
    }

    private async Task<List<AnswerScoreDto>> _GetAnswersAsync(
        List<(string answer, int score)> ratedAnswers,
        string message,
        string mainCategory,
        string targetAudience)
    {
        var systemPrompt = _BuildAnswersPrompt(ratedAnswers);
        
        var candidates = ratedAnswers
            .Select(a => a.answer)
            .ToList();
        
        Logger.LogJson("Кандидаты", candidates);
        
        var response = await _aiClient.Ask(systemPrompt, message);
        
        var indexes = JsonSerializer.Deserialize<List<int>>(response);
        if(indexes is null || indexes.Count == 0) throw new DataException("Cant get answers for message");
        Logger.LogJson("indexes", indexes);
        
        var chosenAnswers = new List<AnswerScoreDto>();
        foreach (var index in indexes)
        {
            chosenAnswers.Add(new AnswerScoreDto(ratedAnswers[index].answer, ratedAnswers[index].score, mainCategory, "", targetAudience));
        }
        return chosenAnswers;
    }

    private string _BuildEntitiesPrompt(List<string> mainCategories, List<string> targetAudiences)
    {
        if (mainCategories.Count == 0) throw new DataException("mainCategories is empty");
        if (targetAudiences.Count == 0) throw new DataException("targetAudiences is empty");

        var mainCandidates = $"[{string.Join(", ", mainCategories)}]";
        var targetCandidates = $"[{string.Join(", ", targetAudiences)}]";
        Logger.Log($"Main category options: {mainCandidates}");
        Logger.Log($"Target audience options: {targetCandidates}");

        var sb = new System.Text.StringBuilder();

        sb.AppendLine($@"
        Determine the MainCategory and TargetAudience of the user's request from the lists below.
        Return strictly a JSON-array of two integers in the format [{MainCategoryLabel}Index, {TargetAudienceLabel}Index].
        Indices start at 0. If there is no suitable value for an element — return -1 for that element.
        Choose TargetAudience only from the provided list. Nothing but the JSON-array should be returned.
        Example: [0,1]
        ");

        sb.AppendLine($"{MainCategoryLabel}:");
        for (int i = 0; i < mainCategories.Count; i++)
        {
            sb.AppendLine($"  {i}: {mainCategories[i]}");
        }

        sb.AppendLine();
        sb.AppendLine($"{TargetAudienceLabel}:");
        for (int i = 0; i < targetAudiences.Count; i++)
        {
            sb.AppendLine($"  {i}: {targetAudiences[i]}");
        }

        sb.AppendLine();
        sb.AppendLine("Selection rules:");
        sb.AppendLine("- 'Новые клиенты' — questions about how to become a client, register, or get the first product.");
        sb.AppendLine("- 'Продукты - Вклады' — everything related to deposits, interest rates, withdrawals, or replenishments.");
        sb.AppendLine("- 'Продукты - Карты' — issuing, receiving, using, blocking, paying, or activating bank cards.");
        sb.AppendLine("- 'Продукты - Кредиты' — loans, credit cards, rates, issuance, repayment, debts.");
        sb.AppendLine("- 'Техническая поддержка' — any technical questions: app, website, online banking, login, installation, errors, updates, downloads, etc.");
        sb.AppendLine("- 'Частные клиенты' — general questions from private individuals not related to specific products.");
        sb.AppendLine();
        sb.AppendLine("Return only the JSON array, for example: [2,0]");

        return sb.ToString();
    }


    
    
    private string _BuildAnswersPrompt(List<(string answer, int score)> ratedAnswers)
    {
        var sb = new System.Text.StringBuilder();
        sb.Append(@"
        For the user's query, choose the most relevant answers and return strictly a JSON array of matching indices.
        Select answers that directly or partially help solve the user's problem.
        There is no limit to the number of answers you can select — choose all that are relevant.
        The listed options have already been pre-filtered by semantic similarity (embeddings), so focus only on relevance to the user's intent.
        Do not include any indices outside the valid range — if there are N options, the maximum possible index is N-1.
        No words, no explanations — only a JSON array. Example: [0,2,3] or [2]

        Options (you must select those that at least partially provide an answer):
        ");
        

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