using TestPlatform2.Data;
using TestPlatform2.Data.Questions;

namespace TestPlatform2.Models;

public class TakeTestViewModel
{
    public string AttemptId { get; set; }
    public Test Test { get; set; }
    public List<Question> Questions { get; set; }
    public int RemainingAttempts { get; set; }  // Add this line
}