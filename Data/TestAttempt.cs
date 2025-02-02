namespace TestPlatform2.Data;

public class TestAttempt
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string FirstName { get; set; } // Student's first name
    public string LastName { get; set; } // Student's last name
    public string StudentEmail { get; set; } // Automatically saved from the invite
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; } // Null if test is abandoned
    public bool IsCompleted { get; set; } = false;
    public double Score { get; set; }
    
    public int RemainingAttempts { get; set; } // Remaining attempts for the test "to cheat"

    // Relationships
    public string TestId { get; set; }
    public Test Test { get; set; }
    public List<Answer> Answers { get; set; } = new();
}