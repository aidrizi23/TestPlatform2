using TestPlatform2.Data;

namespace TestPlatform2.Models;

public class TestViewModel
{
    public string Title { get; set; }
    public string Description { get; set; }
    public bool RandomizeQuestions { get; set; }
    public int TimeLimit { get; set; }
    public int MaxAttempts { get; set; }
    public User Creator { get; set; }
    
}