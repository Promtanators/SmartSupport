using SupportApi.Models.Entities.Enums;

namespace SupportApi.Models.Entities;

public class BankFaq
{
    public string MainCategory { get; set; }
    public string Subcategory  { get; set; }
    public string ExampleQuestion  { get; set; }
    public string Priority  { get; set; }
    public string TargetAudience  { get; set; }
    public string TemplateResponse   { get; set; }
    
    public string? ExampleEmbedding { get; set; }
    public BankFaq(
        string mainCategory,
        string subcategory, 
        string exampleQuestion,
        string priority,
        string targetAudience, 
        string templateResponse,
        string exampleEmbedding
        )
    {
        MainCategory = mainCategory;
        Subcategory = subcategory;
        ExampleQuestion = exampleQuestion;
        Priority = priority;
        TargetAudience = targetAudience;
        TemplateResponse = templateResponse;
        ExampleEmbedding = exampleEmbedding;
    }
}