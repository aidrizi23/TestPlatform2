namespace TestPlatform2.Data.Questions;

public abstract class Question
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Text { get; set; }
    public double Points { get; set; } = 1;
    public int Position { get; set; }
    
    public string TestId { get; set; }
    public Test Test { get; set; }
    
    // Common method for all questions
    public abstract bool ValidateAnswer(object answer);
}