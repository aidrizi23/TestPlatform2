using TestPlatform2.Data;

namespace TestPlatform2.Models;

public class TestAttemptsViewModel
{
    public string TestId { get; set; }
    public IEnumerable<TestAttempt> AllAttempts { get; set; }
    public IEnumerable<TestAttempt> FinishedAttempts { get; set; }
    public IEnumerable<TestAttempt> UnfinishedAttempts { get; set; }
    
    public string CurrentFilter { get; set; } // To track which filter is active

}