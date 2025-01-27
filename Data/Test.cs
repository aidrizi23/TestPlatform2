using System.ComponentModel.DataAnnotations.Schema;
using TestPlatform2.Data.Questions;

namespace TestPlatform2.Data;

public class Test
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    public string TestName { get; set; }
    public string Description { get; set; }
    public bool RandomizeQuestions { get; set; }
    
    public int TimeLimit { get; set; }
    public int MaxAttempts { get; set; }
    
    
    
    // navigation property to the user who created the test
    [ForeignKey("UserId")]
    public string UserId { get; set; }
    public User User { get; set; }
    
    // navigation property to the questions in the test
    public virtual ICollection<Question> Questions { get; set; }
}