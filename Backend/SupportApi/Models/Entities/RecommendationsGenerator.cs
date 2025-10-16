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
    private const int TrustScore = 85;

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
        
        var embeddingValues = _RateByEmbedding(_bankFaqs, embeddingResult);
        
        var matchList = embeddingValues
            .OrderByDescending(x => x.Item2)
            .Take(MaxTemplates)
            .ToList();
        
        return matchList
            .Select(x => new AnswerScoreDto(x.answer, x.score, "", "", ""))
            .ToList();
    }
    
    public async Task<List<AnswerScoreDto>> GetRecommendations(string message)
    {
        var mainCategories = await _bankFaqs
            .Select(b => b.MainCategory)
            .Distinct()
            .ToListAsync();
        
        var messageEmbTask = _sciBoxClient.GetEmbeddingAsync(message);
        var mainCategoryTask = GetMainCategoryAsync(mainCategories, message);
        await Task.WhenAll(messageEmbTask, mainCategoryTask);

        var messageEmbJson = await messageEmbTask;
        var mainCategory = await mainCategoryTask;
        
        var messageEmb = JsonSerializer.Deserialize<double[]>(messageEmbJson);
        if (messageEmb is null) throw new DataException("Cant get embeddings for message");
        
        var ratedAnswers = _RateByEmbedding(_bankFaqs, messageEmb);
        var trustedAnswers = ratedAnswers
            .Where(a => a.score >= TrustScore)
            .Select(a => new AnswerScoreDto(a.answer, a.score, mainCategory ?? "", "", ""))
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
        return await _GetAnswersAsync(ratedAnswers, message, mainCategory);
    }
    
    private List<(string answer, int score)> _RateByEmbedding(
        IEnumerable<BankFaq> bankFaqs,
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
    

    public async Task<string?> GetMainCategoryAsync(
        List<string> mainCategories,
        string message)
    {
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
        string message,
        string mainCategory)
    {
        var systemPrompt = _BuildAnswersPrompt(ratedAnswers);
        
        var candidates = ratedAnswers
            .Select(a => a.answer)
            .ToList();
        
        Logger.LogJson("Кандидаты", candidates);
        
        var response = await _sciBoxClient.Ask(systemPrompt, message);
        
        var indexes = JsonSerializer.Deserialize<List<int>>(response);
        if(indexes is null || indexes.Count == 0) throw new DataException("Cant get answers for message");
        Logger.LogJson("indexes", indexes);
        
        var chosenAnswers = new List<AnswerScoreDto>();
        foreach (var index in indexes)
        {
            chosenAnswers.Add(new AnswerScoreDto(ratedAnswers[index].answer, ratedAnswers[index].score, mainCategory, "", ""));
        }
        return chosenAnswers;
    }

    private string _BuildMainCategoryPrompt(List<string> mainCategories)
    {
        if (mainCategories.Count == 0) throw new DataException("mainCategories is empty");
        var candidates = $"[{string.Join(",", mainCategories)}]";
        Logger.Log($"Main category options: {candidates}");
        var sb = new System.Text.StringBuilder();

        sb.AppendLine($@"
        Determine the main category of the user's request from the list of categories:
        {candidates}

        Selection rules:
        - 'Новые клиенты' — questions about how to become a client, register, or get the first product.
        - 'Продукты - Вклады' — everything related to deposits, interest rates, withdrawals, or replenishments.
        - 'Продукты - Карты' — issuing, receiving, using, blocking, paying, or activating bank cards.
        - 'Продукты - Кредиты' — loans, credit cards, rates, issuance, repayment, debts.
        - 'Техническая поддержка' — any technical questions: app, website, online banking, login, installation, errors, updates, downloads, etc.
        - 'Частные клиенты' — general questions from private individuals not related to specific products.

        Respond with exactly one number — the category index.
        Valid indices are from 0 to {mainCategories.Count - 1}.
        If there is no suitable category, return -1.
        No explanations, only the number.
        ");


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