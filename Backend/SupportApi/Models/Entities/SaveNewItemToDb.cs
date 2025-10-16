using Microsoft.EntityFrameworkCore;

namespace SupportApi.Models.Entities;

public class SaveNewItemToDb
{
    private DbSet<BankFaq> _bankFaqs;
    private SciBoxClient _sciBoxClient;
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
        SciBoxClient sciBoxClient)
    {
        _bankFaqs = bankFaqs;
        _operatorResponse = operatorResponse;
        _userResponse = userResponse;
        _mainCategory = mainCategory;
        _sciBoxClient = sciBoxClient;
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
        var embedding = await _sciBoxClient.GetEmbeddingAsync(userResponse);
        var newFaq = new BankFaq
        (
            _mainCategory,
            null, 
            userResponse,
            null,
            _targetAudience,
            operatorResponse,
            embedding
        );
        await _bankFaqs.AddAsync(newFaq);
    }
}