using TestPlatform2.Data.Questions;

namespace TestPlatform2.Data;

public class Answer
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string? Response { get; set; } // JSON or string (e.g., "true", "A,B", "short answer")
    public double PointsAwarded { get; set; }
    

    // Relationships
    public string QuestionId { get; set; }
    public Question Question { get; set; }
    public string AttemptId { get; set; }
    public TestAttempt Attempt { get; set; }
}