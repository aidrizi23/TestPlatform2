namespace TestPlatform2.Data;

public class TestAttempt
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string StudentName { get; set; } // Collected at test start
    public string StudentEmail { get; set; } // Collected at test start
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; } // Null if test is abandoned
    public bool IsCompleted { get; set; } = false;

    // Relationships
    public string TestId { get; set; }
    public Test Test { get; set; }
    public List<Answer> Answers { get; set; } = new();
}