namespace TestPlatform2.Data.Questions;

public class ChoiceOption
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Text { get; set; }
    public string QuestionId { get; set; }
}