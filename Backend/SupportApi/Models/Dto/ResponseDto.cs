namespace SupportApi.Models.Dto;

public record AnswerScoreDto(string Answer, int Score, string MainCategory, string SubCategory, string TargetAudience);

public record ResponseDto(List<AnswerScoreDto> Recommendations);