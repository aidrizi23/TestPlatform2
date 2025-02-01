using Microsoft.AspNetCore.Mvc;
using TestPlatform2.Data;
using TestPlatform2.Models;
using TestPlatform2.Repository;

namespace TestPlatform2.Controllers;

public class TestAttemptController : Controller
{
    private readonly ITestRepository _testRepository;
    private readonly ITestInviteRepository _inviteRepository;
    private readonly ITestAttemptRepository _attemptRepository;
    private readonly IAnswerRepository _answerRepository;

    public TestAttemptController(
        ITestRepository testRepository,
        ITestInviteRepository inviteRepository,
        ITestAttemptRepository attemptRepository,
        IAnswerRepository answerRepository)
    {
        _testRepository = testRepository;
        _inviteRepository = inviteRepository;
        _attemptRepository = attemptRepository;
        _answerRepository = answerRepository; 
    }

    [HttpGet]
    [HttpGet]
    public async Task<IActionResult> StartTest(string testId, string token)
    {
        // Validate the invite token
        var invite = await _inviteRepository.GetInviteByTokenAsync(token);
        if (invite == null || invite.IsUsed)
            return Forbid();

        // Get the test details
        var test = await _testRepository.GetTestByIdAsync(testId);
        if (test == null)
            return NotFound();

        // Show the start test view
        return View(new StartTestViewModel
        {
            TestId = testId,
            Token = token
        });
    }

    [HttpPost]
    public async Task<IActionResult> StartTest(StartTestViewModel model)
    {
        // Validate the invite token again
        var invite = await _inviteRepository.GetInviteByTokenAsync(model.Token);
        if (invite == null || invite.IsUsed)
            return Forbid();

        // Create a new test attempt
        var attempt = new TestAttempt
        {
            TestId = model.TestId,
            FirstName = model.FirstName,
            LastName = model.LastName,
            StudentEmail = invite.Email, // Automatically save the email from the invite
            StartTime = DateTime.UtcNow,
            IsCompleted = false
        };
        await _attemptRepository.Create(attempt);

        // Mark the invite as used
        await _inviteRepository.MarkAsUsed(invite.Id);

        // Redirect to the test-taking page
        return RedirectToAction("TakeTest", new { attemptId = attempt.Id });
    }

    // [HttpGet]
    // public async Task<IActionResult> TakeTest(string attemptId)
    // {
    //     // Get the test attempt
    //     var attempt = await _attemptRepository.GetAttemptByIdAsync(attemptId);
    //     if (attempt == null || attempt.IsCompleted)
    //         return NotFound();
    //
    //     // Get the test and questions
    //     var test = await _testRepository.GetTestByIdAsync(attempt.TestId);
    //     // check if the test randomizes questions
    //     var questions = test.Questions.OrderBy(q => q.Position).ToList();
    //     if (test.RandomizeQuestions)
    //     {
    //         var random = new Random();
    //         questions = test.Questions.OrderBy(q => random.Next()).ToList();
    //     }
    //     
    //    
    //
    //     // Show the test-taking view
    //     return View(new TakeTestViewModel
    //     {
    //         AttemptId = attemptId,
    //         Questions = questions
    //     });
    // }
    
    [HttpGet]
    public async Task<IActionResult> TakeTest(string attemptId)
    {
        // Get the test attempt
        var attempt = await _attemptRepository.GetAttemptByIdAsync(attemptId);
        if (attempt == null || attempt.IsCompleted)
            return NotFound();

        // Get the test and questions
        var test = await _testRepository.GetTestByIdAsync(attempt.TestId);
        if (test == null)
            return NotFound();

        // Randomize questions if needed
        var questions = test.Questions.OrderBy(q => q.Position).ToList();
        if (test.RandomizeQuestions)
        {
            var random = new Random();
            questions = questions.OrderBy(q => random.Next()).ToList();
        }

        // Show the test-taking view
        return View(new TakeTestViewModel
        {
            AttemptId = attemptId,
            Test = test,
            Questions = questions
        });
    } 

    [HttpPost]
    public async Task<IActionResult> SubmitAnswers(string attemptId, List<AnswerViewModel> answers)
    {
        // Get the test attempt
        var attempt = await _attemptRepository.GetAttemptByIdAsync(attemptId);
        if (attempt == null || attempt.IsCompleted)
            return NotFound();

        // Get the test and questions
        var test = await _testRepository.GetTestByIdAsync(attempt.TestId);
        if (test == null)
            return NotFound();

        // Initialize total score
        double totalScore = 0;

        // Save and grade the answers
        foreach (var answer in answers)
        {
            // Get the question
            var question = test.Questions.FirstOrDefault(q => q.Id == answer.QuestionId);
            if (question == null)
                continue;

            // Validate the answer and calculate points
            double pointsAwarded = 0;
            if (question.ValidateAnswer(answer.Response))
            {
                pointsAwarded = question.Points;
            }

            // Save the answer
            var newAnswer = new Answer
            {
                AttemptId = attemptId,
                QuestionId = answer.QuestionId,
                Response = answer.Response,
                PointsAwarded = pointsAwarded
            };
            await _answerRepository.Create(newAnswer);

            // Add to total score
            totalScore += pointsAwarded;
        }

        // Update the attempt with the total score
        attempt.Score = totalScore;
        attempt.IsCompleted = true;
        attempt.EndTime = DateTime.UtcNow;
        await _attemptRepository.Update(attempt);

        // Redirect to the test completed page
        return RedirectToAction("TestCompleted");
    }

    [HttpGet]
    public IActionResult TestCompleted()
    {
        return View();
    }
    
    // [HttpPost]
    // public async Task<IActionResult> IncrementAttempt(string attemptId)
    // {
    //     var attempt = await _attemptRepository.GetAttemptByIdAsync(attemptId);
    //     if (attempt == null || attempt.IsCompleted)
    //         return NotFound();
    //
    //     // Increment the attempt number
    //     attempt.AttemptNumber++;
    //     await _attemptRepository.Update(attempt);
    //
    //     return Ok();
    // }
}
