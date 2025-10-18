using System.ComponentModel.DataAnnotations;

namespace SupportApi.Models.Entities;


public class BankFaq
{
    [MaxLength(64)]
    public string MainCategory { get; set; }
    
    [MaxLength(64)]
    public string? Subcategory  { get; set; }
    
    [MaxLength(1024)]
    public string ExampleQuestion  { get; set; }
    
    [MaxLength(10)]
    public string? Priority  { get; set; }
    
    [MaxLength(64)]
    public string TargetAudience  { get; set; }
    
    [MaxLength(1024)]
    public string TemplateResponse   { get; set; }
    
    [MaxLength(30000)]
    public string? ExampleSciBoxEmbedding { get; set; }
    
    [MaxLength(30000)]
    public string? ExampleMistralEmbedding { get; set; }
    
    public BankFaq(
        string mainCategory,
        string? subcategory, 
        string exampleQuestion,
        string? priority,
        string targetAudience, 
        string templateResponse,
        string? exampleSciBoxEmbedding = null,
        string? exampleMistralEmbedding = null
        )
    {
        MainCategory = mainCategory;
        Subcategory = subcategory;
        ExampleQuestion = exampleQuestion;
        Priority = priority;
        TargetAudience = targetAudience;
        TemplateResponse = templateResponse;
        ExampleSciBoxEmbedding = exampleSciBoxEmbedding;
        ExampleMistralEmbedding = exampleMistralEmbedding;
    }
}