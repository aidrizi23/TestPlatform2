namespace TestPlatform2.Models.Questions;

public class ShortAnswerQuestionDto : QuestionDto
{
    public string ExpectedAnswer { get; set; }
    public bool CaseSensitive { get; set; }
}