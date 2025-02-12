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

    // Correctly map the foreign key
    public string UserId { get; set; }
    public User User { get; set; }

    public virtual ICollection<Question> Questions { get; set; } = new List<Question>();
    
    public List<TestInvite> InvitedStudents { get; set; } = new();
    public List<TestAttempt> Attempts { get; set; } = new();
    
    
    // properties to set the questions that will be shown on each category
    public int QuestionsToShow { get; set; }
    
    public int? MultipleChoiceQuestionsToShow { get; set; }
    public int? TrueFalseQuestionsToShow { get; set; }
    public int? ShortAnswerQuestionsToShow { get; set; } 
    // we can add even more properties later but for now there will only be 3 types of questions.
    
    
    
}
