using Microsoft.EntityFrameworkCore;

namespace SupportApi.Models.Entities;

public class SaveNewItemToDb
{
    private DbSet<BankFaq> _bankFaqs;
    private SciBoxClient _sciBoxClient;
    private string _operatorResponse;
    private string _userResponse;
    private string _mainCategory;

    public SaveNewItemToDb(DbSet<BankFaq> bankFaqs, string operatorResponse, string userResponse, string mainCategory, SciBoxClient sciBoxClient)
    {
        _bankFaqs = bankFaqs;
        _operatorResponse = operatorResponse;
        _userResponse = userResponse;
        _mainCategory = mainCategory;
        _sciBoxClient = sciBoxClient;
    }

    public async void AnalysisAndSave()
    {
        var result = await _bankFaqs.Where(a => a.ExampleQuestion.Contains(_userResponse) || a.TemplateResponse.Contains(_operatorResponse)).ToListAsync();
        if (!(result.Count > 0))
        {
            _SaveToDb(_operatorResponse,  _userResponse);
        }
    }

    private async void _SaveToDb(string operatorResponse,  string userResponse)
    {
        var embending = _sciBoxClient.GetEmbeddingAsync(userResponse).Result;
        var newFaq = new BankFaq
        (
            _mainCategory,
            null, 
            userResponse,
            null,
            null,
            operatorResponse,
            embending
        );
        await _bankFaqs.AddAsync(newFaq);
    }
}