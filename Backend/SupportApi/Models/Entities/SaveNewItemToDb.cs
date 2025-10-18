using Microsoft.EntityFrameworkCore;
using SupportApi.Services;

namespace SupportApi.Models.Entities;

public class SaveNewItemToDb
{
    private DbSet<BankFaq> _bankFaqs;
    private AiClient _aiClient;
    private string _operatorResponse;
    private string _userResponse;
    private string _mainCategory;
    private string _targetAudience;

    public SaveNewItemToDb(
        DbSet<BankFaq> bankFaqs,
        string operatorResponse,
        string userResponse,
        string mainCategory,
        string targetAudience,
        AiClient aiClient)
    {
        _bankFaqs = bankFaqs;
        _operatorResponse = operatorResponse;
        _userResponse = userResponse;
        _mainCategory = mainCategory;
        _aiClient = aiClient;
        _targetAudience = targetAudience;
    }

    public async Task AnalysisAndSave()
    {
        var result = await _bankFaqs.Where(a => a.ExampleQuestion.Contains(_userResponse) || a.TemplateResponse.Contains(_operatorResponse)).ToListAsync();
        if (!(result.Count > 0))
        {
            await _SaveToDb(_operatorResponse,  _userResponse);
        }
    }

    private async Task _SaveToDb(string operatorResponse,  string userResponse)
    {
        var embedding = await _aiClient.GetEmbeddingAsync(userResponse);
        var newFaq = new BankFaq
        (
            _mainCategory,
            null, 
            userResponse,
            null,
            _targetAudience,
            operatorResponse,
            exampleSciBoxEmbedding: _aiClient.IsMistral ? null : embedding,
            exampleMistralEmbedding:_aiClient.IsMistral ? embedding : null
        );
        await _bankFaqs.AddAsync(newFaq);
    }
}