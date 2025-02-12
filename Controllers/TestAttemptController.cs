// using Microsoft.AspNetCore.Mvc;
// using TestPlatform2.Data;
// using TestPlatform2.Data.Questions;
// using TestPlatform2.Models;
// using TestPlatform2.Repository;
//
// namespace TestPlatform2.Controllers;
//
// public class TestAttemptController : Controller
// {
//     private readonly ITestRepository _testRepository;
//     private readonly ITestInviteRepository _inviteRepository;
//     private readonly ITestAttemptRepository _attemptRepository;
//     private readonly IAnswerRepository _answerRepository;
//
//     public TestAttemptController(
//         ITestRepository testRepository,
//         ITestInviteRepository inviteRepository,
//         ITestAttemptRepository attemptRepository,
//         IAnswerRepository answerRepository)
//     {
//         _testRepository = testRepository;
//         _inviteRepository = inviteRepository;
//         _attemptRepository = attemptRepository;
//         _answerRepository = answerRepository; 
//     }
//
//     [HttpGet]
//     [HttpGet]
//     public async Task<IActionResult> StartTest(string testId, string token)
//     {
//         // Validate the invite token
//         var invite = await _inviteRepository.GetInviteByTokenAsync(token);
//         if (invite == null || invite.IsUsed)
//             return Forbid();
//
//         // Get the test details
//         var test = await _testRepository.GetTestByIdAsync(testId);
//         if (test == null)
//             return NotFound();
//
//         // Show the start test view
//         return View(new StartTestViewModel
//         {
//             TestId = testId,
//             Token = token
//         });
//     }
//
//     [HttpPost]
//     public async Task<IActionResult> StartTest(StartTestViewModel model)
//     {
//         // Validate the invite token again
//         var invite = await _inviteRepository.GetInviteByTokenAsync(model.Token);
//         if (invite == null || invite.IsUsed)
//             return Forbid();
//         
//         var test = await _testRepository.GetTestByIdAsync(model.TestId);
//         if (test == null)
//             return NotFound();
//
//         // Create a new test attempt
//         var attempt = new TestAttempt
//         {
//             TestId = model.TestId,
//             FirstName = model.FirstName,
//             LastName = model.LastName,
//             StudentEmail = invite.Email, // Automatically save the email from the invite
//             StartTime = DateTime.UtcNow,
//             IsCompleted = false,
//             RemainingAttempts = test.MaxAttempts
//         };
//         await _attemptRepository.Create(attempt);
//
//         // Mark the invite as used
//         await _inviteRepository.MarkAsUsed(invite.Id);
//
//         // Redirect to the test-taking page
//         return RedirectToAction("TakeTest", new { attemptId = attempt.Id });
//     }
//
//     
//     // [HttpGet]
//     // public async Task<IActionResult> TakeTest(string attemptId)
//     // {
//     //     // Get the test attempt
//     //     var attempt = await _attemptRepository.GetAttemptByIdAsync(attemptId);
//     //     if (attempt == null || attempt.IsCompleted)
//     //         return NotFound();
//     //
//     //     // Get the test and questions
//     //     var test = await _testRepository.GetTestByIdAsync(attempt.TestId);
//     //     if (test == null)
//     //         return NotFound();
//     //     
//     //     if(attempt.RemainingAttempts <= 0)
//     //         return RedirectToAction("TestCompleted");
//     //
//     //     // Randomize questions if needed
//     //     var questions = test.Questions.OrderBy(q => q.Position).ToList();
//     //     if (test.RandomizeQuestions)
//     //     {
//     //         var random = new Random();
//     //         questions = questions.OrderBy(q => random.Next()).ToList();
//     //     }
//     //     
//     //     return View(new TakeTestViewModel
//     //     {
//     //         AttemptId = attemptId,
//     //         Test = test,
//     //         Questions = questions,
//     //         RemainingAttempts = test.MaxAttempts 
//     //     });
//     // } 
//     
//     
//     [HttpGet]
// public async Task<IActionResult> TakeTest(string attemptId)
// {
//     // Get the test attempt
//     var attempt = await _attemptRepository.GetAttemptByIdAsync(attemptId);
//     if (attempt == null || attempt.IsCompleted)
//         return NotFound();
//
//     // Get the test and questions
//     var test = await _testRepository.GetTestByIdAsync(attempt.TestId);
//     if (test == null)
//         return NotFound();
//
//     if (attempt.RemainingAttempts <= 0)
//         return RedirectToAction("TestCompleted");
//
//     // 1. Select Questions Based on Type Counts
//     var selectedQuestions = new List<Question>();
//
//     if (test.MultipleChoiceQuestionsToShow.HasValue)
//     {
//         selectedQuestions.AddRange(test.Questions
//             .OfType<MultipleChoiceQuestion>()
//             .OrderBy(q => Guid.NewGuid())
//             .Take(test.MultipleChoiceQuestionsToShow.Value)
//             .ToList());
//     }
//
//     if (test.TrueFalseQuestionsToShow.HasValue)
//     {
//         selectedQuestions.AddRange(test.Questions
//             .OfType<TrueFalseQuestion>()
//             .OrderBy(q => Guid.NewGuid())
//             .Take(test.TrueFalseQuestionsToShow.Value)
//             .ToList());
//     }
//
//     if (test.ShortAnswerQuestionsToShow.HasValue)
//     {
//         selectedQuestions.AddRange(test.Questions
//             .OfType<ShortAnswerQuestion>()
//             .OrderBy(q => Guid.NewGuid())
//             .Take(test.ShortAnswerQuestionsToShow.Value)
//             .ToList());
//     }
//
//     // If no number of question per type selected, return the set amount of questions
//     if (!test.MultipleChoiceQuestionsToShow.HasValue && !test.TrueFalseQuestionsToShow.HasValue &&
//         !test.ShortAnswerQuestionsToShow.HasValue)
//     {
//         selectedQuestions = test.Questions.OrderBy(q => Guid.NewGuid()).Take(test.QuestionsToShow).ToList();
//     }
//
//     // 2. Randomize Questions (if needed)
//     if (test.RandomizeQuestions)
//     {
//         selectedQuestions = selectedQuestions.OrderBy(q => Guid.NewGuid()).ToList();
//     }
//
//     // 3. Pass the Selected Questions to the View
//     return View(new TakeTestViewModel
//     {
//         AttemptId = attemptId,
//         Test = test,
//         Questions = selectedQuestions, // Pass the *selected* questions
//         RemainingAttempts = test.MaxAttempts
//     });
// }
//     public class UpdateAttemptsRequest
//     {
//         public string AttemptId { get; set; }
//         public int RemainingAttempts { get; set; }
//     }
//
//     [HttpPost]
//     public async Task<IActionResult> UpdateRemainingAttempts([FromBody] UpdateAttemptsRequest request)
//     {
//         if (string.IsNullOrEmpty(request.AttemptId)) 
//             return BadRequest("Invalid attempt ID");
//
//         var attempt = await _attemptRepository.GetAttemptByIdAsync(request.AttemptId);
//         if (attempt == null) return NotFound();
//
//         attempt.RemainingAttempts = request.RemainingAttempts;
//         await _attemptRepository.Update(attempt);
//
//         return Ok();
//     }
//     
//     
//     [HttpPost]
//     public async Task<IActionResult> SubmitAnswers(string attemptId, List<AnswerViewModel> answers)
//     {
//         // Get the test attempt
//         var attempt = await _attemptRepository.GetAttemptByIdAsync(attemptId);
//         if (attempt == null || attempt.IsCompleted)
//             return NotFound();
//
//         // Get the test and questions
//         var test = await _testRepository.GetTestByIdAsync(attempt.TestId);
//         if (test == null)
//             return NotFound();
//
//         // Initialize total score
//         double totalScore = 0;
//
//         // Save and grade the answers
//         foreach (var answer in answers)
//         {
//             // Get the question
//             var question = test.Questions.FirstOrDefault(q => q.Id == answer.QuestionId);
//             if (question == null)
//                 continue;
//
//             // Parse the answer based on the question type
//             object parsedAnswer = null;
//             switch (question)
//             {
//                 case TrueFalseQuestion tfq:
//                     if (bool.TryParse(answer.Response, out bool boolAnswer))
//                         parsedAnswer = boolAnswer;
//                     break;
//
//                 case ShortAnswerQuestion saq:
//                     parsedAnswer = answer.Response;
//                     break;
//
//                 case MultipleChoiceQuestion mcq:
//                     if (answer.Response != null)
//                     {
//                         var selectedAnswers = answer.Response.Split(',').ToList();
//                         parsedAnswer = selectedAnswers;
//                     }
//                     break;
//             }
//
//             // Validate the answer and calculate points
//             double pointsAwarded = 0;
//             if (parsedAnswer != null && question.ValidateAnswer(parsedAnswer))
//             {
//                 pointsAwarded = question.Points;
//             }
//
//             // Save the answer
//             var newAnswer = new Answer
//             {
//                 AttemptId = attemptId,
//                 QuestionId = answer.QuestionId,
//                 Response = answer.Response,
//                 PointsAwarded = pointsAwarded
//             };
//             await _answerRepository.Create(newAnswer);
//
//             // Add to total score
//             totalScore += pointsAwarded;
//         }
//
//         // Update the attempt with the total score
//         attempt.Score = totalScore;
//         attempt.IsCompleted = true;
//         attempt.EndTime = DateTime.UtcNow;
//         await _attemptRepository.Update(attempt);
//
//         // Redirect to the test completed page
//         return RedirectToAction("TestCompleted" , new { attemptId });
//     }
//
//     [HttpGet]
//     public async Task<IActionResult> TestCompleted(string attemptId)
//     {
//         // Get the test attempt
//         var attempt = await _attemptRepository.GetAttemptByIdAsync(attemptId);
//         if (attempt == null)
//             return NotFound();
//
//         // Get the associated test details
//         var test = await _testRepository.GetTestByIdAsync(attempt.TestId);
//         if (test == null)
//             return NotFound();
//
//         // Calculate total points for the test
//         double totalPoints = test.Questions.Sum(q => q.Points);
//
//         // Calculate time taken
//         var timeTaken = attempt.EndTime - attempt.StartTime;
//
//         // Pass data to the view
//         var viewModel = new TestCompletedViewModel
//         {
//             AttemptId = attempt.Id,
//             TestName = test.TestName,
//             Description = test.Description,
//             Score = attempt.Score,
//             TotalPoints = totalPoints,
//             TimeTaken = timeTaken ?? TimeSpan.Zero,
//             FirstName = attempt.FirstName,
//             LastName = attempt.LastName,
//             StudentEmail = attempt.StudentEmail,
//             EndTime = attempt.EndTime ?? DateTime.UtcNow
//         };
//
//         return View(viewModel);
//     }   
//     
// }



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

        var selectedQuestions = new List<Question>();

        //Select questions by type
        if (test.MultipleChoiceQuestionsToShow.HasValue)
        {
            selectedQuestions.AddRange(test.Questions
                .OfType<MultipleChoiceQuestion>()
                .OrderBy(q => Guid.NewGuid())
                .Take(test.MultipleChoiceQuestionsToShow.Value)
                .ToList());
        }

        if (test.TrueFalseQuestionsToShow.HasValue)
        {
            selectedQuestions.AddRange(test.Questions
                .OfType<TrueFalseQuestion>()
                .OrderBy(q => Guid.NewGuid())
                .Take(test.TrueFalseQuestionsToShow.Value)
                .ToList());
        }

        if (test.ShortAnswerQuestionsToShow.HasValue)
        {
            selectedQuestions.AddRange(test.Questions
                .OfType<ShortAnswerQuestion>()
                .OrderBy(q => Guid.NewGuid())
                .Take(test.ShortAnswerQuestionsToShow.Value)
                .ToList());
        }

        //If no number of question per type selected, return the set amount of questions
        if (!test.MultipleChoiceQuestionsToShow.HasValue && !test.TrueFalseQuestionsToShow.HasValue &&
            !test.ShortAnswerQuestionsToShow.HasValue)
        {
            selectedQuestions = test.Questions.OrderBy(q => Guid.NewGuid()).Take(test.QuestionsToShow).ToList();
        }

        if (test.RandomizeQuestions)
        {
            selectedQuestions = selectedQuestions.OrderBy(q => Guid.NewGuid()).ToList();
        }

        var viewModel = new StartTestViewModel
        {
            TestId = testId,
            Token = token,
            Questions = selectedQuestions
        };

        return View(viewModel);
    }

    [HttpPost]
    public async Task<IActionResult> StartTest(StartTestViewModel model)
    {
        // Validate the invite token again
        var invite = await _inviteRepository.GetInviteByTokenAsync(model.Token);
        if (invite == null || invite.IsUsed)
            return Forbid();
        
        var test = await _testRepository.GetTestByIdAsync(model.TestId);
        if (test == null)
            return NotFound();

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

        if (attempt.RemainingAttempts <= 0)
            return RedirectToAction("TestCompleted");

        // 1. Select Questions Based on Type Counts
        var selectedQuestions = new List<Question>();

        if (test.MultipleChoiceQuestionsToShow.HasValue)
        {
            selectedQuestions.AddRange(test.Questions
                .OfType<MultipleChoiceQuestion>()
                .OrderBy(q => Guid.NewGuid())
                .Take(test.MultipleChoiceQuestionsToShow.Value)
                .ToList());
        }

        if (test.TrueFalseQuestionsToShow.HasValue)
        {
            selectedQuestions.AddRange(test.Questions
                .OfType<TrueFalseQuestion>()
                .OrderBy(q => Guid.NewGuid())
                .Take(test.TrueFalseQuestionsToShow.Value)
                .ToList());
        }

        if (test.ShortAnswerQuestionsToShow.HasValue)
        {
            selectedQuestions.AddRange(test.Questions
                .OfType<ShortAnswerQuestion>()
                .OrderBy(q => Guid.NewGuid())
                .Take(test.ShortAnswerQuestionsToShow.Value)
                .ToList());
        }

        // If no number of question per type selected, return the set amount of questions
        if (!test.MultipleChoiceQuestionsToShow.HasValue && !test.TrueFalseQuestionsToShow.HasValue &&
            !test.ShortAnswerQuestionsToShow.HasValue)
        {
            selectedQuestions = test.Questions.OrderBy(q => Guid.NewGuid()).Take(test.QuestionsToShow).ToList();
        }

        // 2. Randomize Questions (if needed)
        if (test.RandomizeQuestions)
        {
            selectedQuestions = selectedQuestions.OrderBy(q => Guid.NewGuid()).ToList();
        }

        // 3. Pass the Selected Questions to the View
        return View(new TakeTestViewModel
        {
            AttemptId = attemptId,
            Test = test,
            Questions = selectedQuestions, // Pass the *selected* questions
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
    
}   