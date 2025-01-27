namespace TestPlatform2.Data.Questions;

public class CreateQuestionViewModel
{
    public string Text { get; set; }
    public double Points { get; set; } = 1;
    public string TestId { get; set; }
    // public string QuestionType { get; set; }
}