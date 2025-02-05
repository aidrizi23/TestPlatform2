namespace TestPlatform2.Models;

public class TestCompletedViewModel
{
    public string AttemptId { get; set; }
    public string TestName { get; set; }
    public string Description { get; set; }
    public double Score { get; set; }
    public double TotalPoints { get; set; }
    public TimeSpan TimeTaken { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string StudentEmail { get; set; }
    public DateTime EndTime { get; set; }
}