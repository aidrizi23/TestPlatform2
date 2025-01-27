namespace TestPlatform2.Data.Questions;

public class TrueFalseQuestion : Question
{
    public bool CorrectAnswer { get; set; }

    public override bool ValidateAnswer(object answer)
    {
        return answer is bool ans && ans == CorrectAnswer;
    }
}