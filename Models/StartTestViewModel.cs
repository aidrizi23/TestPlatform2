using TestPlatform2.Data.Questions;

namespace TestPlatform2.Models;

public class StartTestViewModel
{
    public string TestId { get; set; }
    public string Token { get; set; }
    public string FirstName { get; set; } // Student's first name
    public string LastName { get; set; } // Student's last name
    
    public List<Question> Questions { get; set; }
}