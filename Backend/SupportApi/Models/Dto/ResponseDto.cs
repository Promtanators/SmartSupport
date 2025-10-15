namespace SupportApi.Models.Dto;

public record AnswerScoreDto(string Answer, int Score);

public record ResponseDto(List<AnswerScoreDto> Recommendations);