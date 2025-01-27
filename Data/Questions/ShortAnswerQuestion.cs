namespace TestPlatform2.Data.Questions;

public class ShortAnswerQuestion : Question
{
    public string? ExpectedAnswer { get; set; }
    public bool CaseSensitive { get; set; }

    public override bool ValidateAnswer(object answer)
    {
        if (answer is not string ans) return false;
        
        return CaseSensitive 
            ? ans == ExpectedAnswer
            : ans.Equals(ExpectedAnswer, StringComparison.OrdinalIgnoreCase);
    }
}