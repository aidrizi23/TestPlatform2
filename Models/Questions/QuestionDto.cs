namespace TestPlatform2.Models.Questions;

public class QuestionDto
{
    public string Id { get; set; }
    public string Text { get; set; }
    public double Points { get; set; }
    public int Position { get; set; }
    public string TestId { get; set; }
    public string QuestionType { get; set; }
}