namespace TestPlatform2.Data.Questions;

public class CreateShortAnswerQuestionViewModel : CreateQuestionViewModel
{
    public string ExpectedAnswer { get; set; }
    public bool CaseSensitive { get; set; }
}