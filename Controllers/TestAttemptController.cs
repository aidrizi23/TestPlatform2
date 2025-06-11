using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TestPlatform2.Data;
using TestPlatform2.Data.Questions;
using TestPlatform2.Models;
using TestPlatform2.Repository;

namespace TestPlatform2.Controllers;

public class TestAttemptController : Controller
{
    private readonly ITestRepository _testRepository;
    private readonly ITestInviteRepository _inviteRepository;
    private readonly ITestAttemptRepository _attemptRepository;
    private readonly IAnswerRepository _answerRepository;
    private readonly UserManager<User> _userManager;

    public TestAttemptController(
        ITestRepository testRepository,
        ITestInviteRepository inviteRepository,
        ITestAttemptRepository attemptRepository,
        IAnswerRepository answerRepository,
        UserManager<User> userManager)
    {
        _testRepository = testRepository;
        _inviteRepository = inviteRepository;
        _attemptRepository = attemptRepository;
        _answerRepository = answerRepository;
        _userManager = userManager;
    }
    
    private IActionResult ShowTestDenied(TestDenialReason reason, string testName = null, string testId = null, Dictionary<string, object> additionalInfo = null)
    {
        var model = new TestDeniedViewModel
        {
            Reason = reason,
            TestName = testName,
            TestId = testId,
            AdditionalInfo = additionalInfo ?? new Dictionary<string, object>()
        };

        switch (reason)
        {
            case TestDenialReason.TestTakenBefore:
                model.Title = "Test Already Completed";
                model.Message = "You have already taken this test and submitted your answers.";
                model.IconClass = "icon-info";
                model.AlertClass = "status-info";
                model.ShowContactSupport = true;
                model.ShowRetryButton = false;
                break;

            case TestDenialReason.InvalidToken:
                model.Title = "Invalid Access Link";
                model.Message = "The test link you used is invalid or has expired. Please check your email for the correct link.";
                model.IconClass = "icon-error";
                model.AlertClass = "status-error";
                model.ShowContactSupport = true;
                model.ShowRetryButton = false;
                break;

            case TestDenialReason.TestLocked:
                model.Title = "Test Currently Locked";
                model.Message = "This test has been temporarily locked by the instructor. Please try again later or contact them directly.";
                model.IconClass = "icon-locked";
                model.AlertClass = "status-warning";
                model.ShowContactSupport = true;
                model.ShowRetryButton = false;
                break;

            case TestDenialReason.TestNotFound:
                model.Title = "Test Not Found";
                model.Message = "The test you're looking for doesn't exist or may have been deleted.";
                model.IconClass = "icon-error";
                model.AlertClass = "status-error";
                model.ShowContactSupport = true;
                model.ShowRetryButton = false;
                break;

            case TestDenialReason.NoQuestionsAvailable:
                model.Title = "Test Not Ready";
                model.Message = "This test doesn't have any questions yet. The instructor needs to add questions before students can take it.";
                model.IconClass = "icon-warning";
                model.AlertClass = "status-warning";
                model.ShowContactSupport = true;
                model.ShowRetryButton = false;
                break;

            case TestDenialReason.MaxAttemptsExceeded:
                model.Title = "Maximum Attempts Reached";
                model.Message = "You have used all available attempts for this test.";
                model.IconClass = "icon-info";
                model.AlertClass = "status-info";
                model.ShowContactSupport = true;
                model.ShowRetryButton = false;
                break;

            case TestDenialReason.InviteAlreadyUsed:
                model.Title = "Invitation Already Used";
                model.Message = "This test invitation has already been used. Each invitation can only be used once.";
                model.IconClass = "icon-info";
                model.AlertClass = "status-info";
                model.ShowContactSupport = true;
                model.ShowRetryButton = false;
                break;

            case TestDenialReason.UnauthorizedAccess:
                model.Title = "Access Denied";
                model.Message = "You don't have permission to access this test.";
                model.IconClass = "icon-error";
                model.AlertClass = "status-error";
                model.ShowContactSupport = true;
                model.ShowRetryButton = false;
                break;

            case TestDenialReason.TestExpired:
                model.Title = "Test Expired";
                model.Message = "This test is no longer available. The submission deadline has passed.";
                model.IconClass = "icon-warning";
                model.AlertClass = "status-warning";
                model.ShowContactSupport = true;
                model.ShowRetryButton = false;
                break;

            default:
                model.Title = "Access Denied";
                model.Message = "You cannot access this test at this time.";
                model.IconClass = "icon-error";
                model.AlertClass = "status-error";
                model.ShowContactSupport = true;
                model.ShowRetryButton = false;
                break;
        }

        return View("TestDenied", model);
    }

    [HttpGet]
    public IActionResult TestDenied()
    {
        // This method handles direct access to the TestDenied page
        return ShowTestDenied(TestDenialReason.UnauthorizedAccess);
    }
    
    [HttpGet]
    public async Task<IActionResult> StartTest(string testId, string token)
    {
        // Validate the invite token
        var invite = await _inviteRepository.GetInviteByTokenAsync(token);
        if (invite == null)
            return ShowTestDenied(TestDenialReason.InvalidToken);
            
        if (invite.IsUsed)
            return ShowTestDenied(TestDenialReason.InviteAlreadyUsed);

        // Get the test details
        var test = await _testRepository.GetTestByIdAsync(testId);
        if (test == null)
            return ShowTestDenied(TestDenialReason.TestNotFound);
            
        if (test.IsLocked)
            return ShowTestDenied(TestDenialReason.TestLocked, test.TestName, testId);

        // Show the start test view
        return View(new StartTestViewModel
        {
            TestId = testId,
            Token = token
        });
    }
    
    // method to start the test by providing just the token in a field
    
    

    [HttpPost]
    public async Task<IActionResult> StartTest(StartTestViewModel model)
    {
        // Validate the invite token again
        var invite = await _inviteRepository.GetInviteByTokenAsync(model.Token);
        if (invite == null)
            return ShowTestDenied(TestDenialReason.InvalidToken);
            
        if (invite.IsUsed)
            return ShowTestDenied(TestDenialReason.InviteAlreadyUsed);
        
        var test = await _testRepository.GetTestByIdAsync(model.TestId);
        if (test == null)
            return ShowTestDenied(TestDenialReason.TestNotFound);
            
        if (test.IsLocked)
            return ShowTestDenied(TestDenialReason.TestLocked, test.TestName, model.TestId);

        if(test.Questions.Count == 0 || !test.Questions.Any())
            return ShowTestDenied(TestDenialReason.NoQuestionsAvailable, test.TestName, model.TestId);
            
        // Create a new test attempt
        var attempt = new TestAttempt
        {
            TestId = model.TestId,
            FirstName = model.FirstName,
            LastName = model.LastName,
            StudentEmail = invite.Email, // Automatically save the email from the invite
            StartTime = DateTime.UtcNow,
            IsCompleted = false,
            RemainingAttempts = test.MaxAttempts
        };
        await _attemptRepository.Create(attempt);

        // Mark the invite as used
        await _inviteRepository.MarkAsUsed(invite.Id);

        // Redirect to the test-taking page
        return RedirectToAction("TakeTest", new { attemptId = attempt.Id });
    }

    [HttpPost]
    public async Task<IActionResult> EnterToken(string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            return ShowTestDenied(TestDenialReason.InvalidToken, additionalInfo: new Dictionary<string, object>
            {
                { "Error", "No token provided" }
            });
        }

        var trimmedToken = token.Trim();
        // Validate the invite token
        var invite = await _inviteRepository.GetInviteByTokenAsync(trimmedToken);
        if (invite == null)
        {
            return ShowTestDenied(TestDenialReason.InvalidToken, additionalInfo: new Dictionary<string, object>
            {
                { "Token", trimmedToken }
            });
        }
        
        if (invite.IsUsed)
        {
            return ShowTestDenied(TestDenialReason.InviteAlreadyUsed, additionalInfo: new Dictionary<string, object>
            {
                { "Token", trimmedToken }
            });
        }

        // Get the test ID associated with the invite.
        var testId = invite.TestId;
        if (string.IsNullOrEmpty(testId))
        {
            return ShowTestDenied(TestDenialReason.TestNotFound, additionalInfo: new Dictionary<string, object>
            {
                { "Token", trimmedToken },
                { "Error", "No test associated with this invite" }
            });
        }

        // Redirect to the StartTest method with the testId and token
        return RedirectToAction("StartTest", new { testId = testId, token = trimmedToken });
    }
    
    
    
    [HttpGet]
    public async Task<IActionResult> TakeTest(string attemptId)
    {
        // Get the test attempt
        var attempt = await _attemptRepository.GetAttemptByIdAsync(attemptId);
        if (attempt == null)
            return ShowTestDenied(TestDenialReason.TestNotFound, additionalInfo: new Dictionary<string, object>
            {
                { "Attempt ID", attemptId }
            });
            
        if (attempt.IsCompleted)
            return ShowTestDenied(TestDenialReason.TestTakenBefore, additionalInfo: new Dictionary<string, object>
            {
                { "Completed On", attempt.EndTime?.ToString("MMMM dd, yyyy 'at' HH:mm") ?? "Unknown" },
                { "Score", attempt.Score.ToString("0.00") }
            });

        // Get the test and questions
        var test = await _testRepository.GetTestByIdAsync(attempt.TestId);
        if (test == null)
            return ShowTestDenied(TestDenialReason.TestNotFound);
            
        if (test.IsLocked)
            return ShowTestDenied(TestDenialReason.TestLocked, test.TestName, test.Id);
        
        if(attempt.RemainingAttempts <= 0)
            return ShowTestDenied(TestDenialReason.MaxAttemptsExceeded, test.TestName, test.Id, new Dictionary<string, object>
            {
                { "Max Attempts", test.MaxAttempts.ToString() }
            });

        // Randomize questions if needed
        var questions = test.Questions.OrderBy(q => q.Position).ToList();
        if (test.RandomizeQuestions)
        {
            var random = new Random();
            questions = questions.OrderBy(q => random.Next()).ToList();
        }
        
        return View(new TakeTestViewModel
        {
            AttemptId = attemptId,
            Test = test,
            Questions = questions,
            RemainingAttempts = test.MaxAttempts 
        });
    } 
    public class UpdateAttemptsRequest
    {
        public string AttemptId { get; set; }
        public int RemainingAttempts { get; set; }
    }

    [HttpPost]
    public async Task<IActionResult> UpdateRemainingAttempts([FromBody] UpdateAttemptsRequest request)
    {
        if (string.IsNullOrEmpty(request.AttemptId)) 
            return BadRequest("Invalid attempt ID");

        var attempt = await _attemptRepository.GetAttemptByIdAsync(request.AttemptId);
        if (attempt == null) return NotFound();

        attempt.RemainingAttempts = request.RemainingAttempts;
        await _attemptRepository.Update(attempt);

        return Ok();
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

            // Parse the answer based on the question type
            object parsedAnswer = null;
            switch (question)
            {
                case TrueFalseQuestion tfq:
                    if (bool.TryParse(answer.Response, out bool boolAnswer))
                        parsedAnswer = boolAnswer;
                    break;

                case ShortAnswerQuestion saq:
                    parsedAnswer = answer.Response;
                    break;

                case MultipleChoiceQuestion mcq:
                    if (answer.Response != null)
                    {
                        var selectedAnswers = answer.Response.Split(',').ToList();
                        parsedAnswer = selectedAnswers;
                    }
                    break;
            }

            // Validate the answer and calculate points
            double pointsAwarded = 0;
            if (parsedAnswer != null && question.ValidateAnswer(parsedAnswer))
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
        return RedirectToAction("TestCompleted" , new { attemptId });
    }

    [HttpGet]
    public async Task<IActionResult> TestCompleted(string attemptId)
    {
        // Get the test attempt
        var attempt = await _attemptRepository.GetAttemptByIdAsync(attemptId);
        if (attempt == null)
            return NotFound();

        // Get the associated test details
        var test = await _testRepository.GetTestByIdAsync(attempt.TestId);
        if (test == null)
            return NotFound();

        // Calculate total points for the test
        double totalPoints = test.Questions.Sum(q => q.Points);

        // Calculate time taken
        var timeTaken = attempt.EndTime - attempt.StartTime;

        // Pass data to the view
        var viewModel = new TestCompletedViewModel
        {
            AttemptId = attempt.Id,
            TestName = test.TestName,
            Description = test.Description,
            Score = attempt.Score,
            TotalPoints = totalPoints,
            TimeTaken = timeTaken ?? TimeSpan.Zero,
            FirstName = attempt.FirstName,
            LastName = attempt.LastName,
            StudentEmail = attempt.StudentEmail,
            EndTime = attempt.EndTime ?? DateTime.UtcNow
        };

        return View(viewModel);
    }   
    
    // method to delete test attempt.
    public async Task<IActionResult> Delete(string id)
    {
        // take the attempt by id
        var attempt = await _attemptRepository.GetAttemptUntrackedByIdAsync(id);
        if (attempt is null) return NotFound();
        
        // get the current user
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return RedirectToAction("Login", "Account");
        
        
        
        // check if the attempt has finished or not
        if (attempt.EndTime is null || !attempt.IsCompleted) return BadRequest("Can not delete an ongoing attempt");
        
        // now delete all the answers of the attempt. to do this we need to get the answers
        var answers = await _answerRepository.GetAnswersByAttemptIdAsync(id);
        // now let's bulk delete the answers

        await _answerRepository.BulkDelete(answers);
        
        // now we need to delete the attempt itself.
        await _attemptRepository.Delete(attempt);

        return RedirectToAction("AllAttempts", "Test", new { testId = attempt.TestId });
    }
    
    [HttpPost]
    public async Task<IActionResult> UpdateAnswerPoints([FromBody] UpdateAnswerPointsRequest request)
    {
        try
        {
            // Get the answer
            var answer = await _answerRepository.GetAnswerByIdAsync(request.AnswerId);
            if (answer == null)
                return NotFound(new { success = false, message = "Answer not found" });

            // Get the current user and verify they own the test
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized(new { success = false, message = "User not authenticated" });

            var attempt = await _attemptRepository.GetAttemptByIdAsync(answer.AttemptId);
            if (attempt == null)
                return NotFound(new { success = false, message = "Test attempt not found" });

            var test = await _testRepository.GetTestByIdAsync(attempt.TestId);
            if (test == null || test.UserId != user.Id)
                return Forbid();

            // Validate points
            if (request.Points < 0)
                return BadRequest(new { success = false, message = "Points cannot be negative" });

            if (request.Points > answer.Question.Points)
                return BadRequest(new { success = false, message = $"Points cannot exceed maximum ({answer.Question.Points})" });

            // Update answer points
            answer.PointsAwarded = request.Points;
            await _answerRepository.Update(answer);

            // Recalculate total score for the attempt
            await RecalculateAttemptScore(attempt.Id);

            return Ok(new { success = true, message = "Points updated successfully", newPoints = request.Points });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "An error occurred while updating points" });
        }
    }

    [HttpPost]
    public async Task<IActionResult> BulkUpdateAnswerPoints([FromBody] BulkUpdateAnswerPointsRequest request)
    {
        try
        {
            // Get current user and verify permissions
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized(new { success = false, message = "User not authenticated" });

            var attempt = await _attemptRepository.GetAttemptByIdAsync(request.AttemptId);
            if (attempt == null)
                return NotFound(new { success = false, message = "Test attempt not found" });

            var test = await _testRepository.GetTestByIdAsync(attempt.TestId);
            if (test == null || test.UserId != user.Id)
                return Forbid();

            // Update each answer
            var updatedCount = 0;
            foreach (var update in request.Updates)
            {
                var answer = await _answerRepository.GetAnswerByIdAsync(update.AnswerId);
                if (answer == null || answer.AttemptId != request.AttemptId)
                    continue;

                // Validate points
                if (update.Points < 0 || update.Points > answer.Question.Points)
                    continue;

                answer.PointsAwarded = update.Points;
                await _answerRepository.Update(answer);
                updatedCount++;
            }

            // Recalculate total score
            await RecalculateAttemptScore(request.AttemptId);

            return Ok(new { success = true, message = $"Updated {updatedCount} answers successfully" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "An error occurred while updating points" });
        }
    }

    [HttpPost]
    public async Task<IActionResult> ResetAnswerToAutoGrade([FromBody] ResetAnswerRequest request)
    {
        try
        {
            // Get the answer
            var answer = await _answerRepository.GetAnswerByIdAsync(request.AnswerId);
            if (answer == null)
                return NotFound(new { success = false, message = "Answer not found" });

            // Get current user and verify permissions
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized(new { success = false, message = "User not authenticated" });

            var attempt = await _attemptRepository.GetAttemptByIdAsync(answer.AttemptId);
            if (attempt == null)
                return NotFound(new { success = false, message = "Test attempt not found" });

            var test = await _testRepository.GetTestByIdAsync(attempt.TestId);
            if (test == null || test.UserId != user.Id)
                return Forbid();

            // Re-grade the answer automatically
            var question = answer.Question;
            double autoGradedPoints = 0;

            if (!string.IsNullOrEmpty(answer.Response))
            {
                object parsedAnswer = null;
                switch (question)
                {
                    case TrueFalseQuestion tfq:
                        if (bool.TryParse(answer.Response, out bool boolAnswer))
                            parsedAnswer = boolAnswer;
                        break;
                    case ShortAnswerQuestion saq:
                        parsedAnswer = answer.Response;
                        break;
                    case MultipleChoiceQuestion mcq:
                        var selectedAnswers = answer.Response.Split(',').Where(a => !string.IsNullOrEmpty(a)).ToList();
                        parsedAnswer = selectedAnswers;
                        break;
                }

                if (parsedAnswer != null && question.ValidateAnswer(parsedAnswer))
                {
                    autoGradedPoints = question.Points;
                }
            }

            // Update answer with auto-graded points
            answer.PointsAwarded = autoGradedPoints;
            await _answerRepository.Update(answer);

            // Recalculate total score
            await RecalculateAttemptScore(attempt.Id);

            return Ok(new { success = true, message = "Answer reset to auto-grade", newPoints = autoGradedPoints });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "An error occurred while resetting answer grade" });
        }
    }

    private async Task RecalculateAttemptScore(string attemptId)
    {
        var attempt = await _attemptRepository.GetAttemptByIdAsync(attemptId);
        if (attempt == null) return;

        var answers = await _answerRepository.GetAnswersByAttemptIdAsync(attemptId);
        var totalScore = answers.Sum(a => a.PointsAwarded);

        attempt.Score = totalScore;
        await _attemptRepository.Update(attempt);
    }

    // method to get all the answers and equivalent questions from testattempt id
    [HttpGet()]
    public async Task<IActionResult> Details(string id)
    {
        // first we get the test AttemptById
        var testAttempt = await _attemptRepository.GetAttemptByIdAsync(id);
        
        // now get all the answers by the test id
        var answers = await _answerRepository.GetAnswersByAttemptIdAsync(id);
        
        return View(answers);
    }

    public class UpdateAnswerPointsRequest
    {
        public string AnswerId { get; set; }
        public double Points { get; set; }
    }

    public class BulkUpdateAnswerPointsRequest
    {
        public string AttemptId { get; set; }
        public List<AnswerPointsUpdate> Updates { get; set; } = new();
    }

    public class AnswerPointsUpdate
    {
        public string AnswerId { get; set; }
        public double Points { get; set; }
    }

    public class ResetAnswerRequest
    {
        public string AnswerId { get; set; }
    }
}