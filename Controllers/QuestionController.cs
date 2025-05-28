using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TestPlatform2.Data;
using TestPlatform2.Data.Questions;
using TestPlatform2.Repository;

namespace TestPlatform2.Controllers;

public class QuestionController : Controller
{
    private readonly IQuestionRepository _questionRepository;
    private readonly UserManager<User> _userManager;
    private readonly ITestRepository _testRepository;
    private readonly ISubscriptionRepository _subscriptionRepository;
    
    public QuestionController(
        IQuestionRepository questionRepository, 
        UserManager<User> userManager, 
        ITestRepository testRepository,
        ISubscriptionRepository subscriptionRepository)
    {
        _questionRepository = questionRepository;
        _userManager = userManager;
        _testRepository = testRepository;
        _subscriptionRepository = subscriptionRepository;
    }
    
    private async Task<IActionResult> CheckQuestionLimitAsync(string userId)
    {
        if (!await _subscriptionRepository.CanCreateQuestionAsync(userId))
        {
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = false, message = "You've reached your free question limit (30 questions). Upgrade to Pro for unlimited questions!" });
            }
            TempData["ErrorMessage"] = "You've reached your free question limit (30 questions). Upgrade to Pro for unlimited questions!";
            return RedirectToAction("Index", "Subscription");
        }
        return null;
    }
    
    // method to delete question
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Delete(string id)
    {
        var question = await _questionRepository.GetQuestionByIdAsync(id);
        if (question is null)
        {
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = false, message = "Question not found" });
            }
            return NotFound();
        }
        
        var test = await _testRepository.GetTestByIdAsync(question.TestId);
        if (test is null)
        {
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = false, message = "Test not found" });
            }
            return NotFound();
        }
        
        var user = await _userManager.GetUserAsync(User);
        if (user is null || test.UserId != user.Id)
        {
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = false, message = "Unauthorized access" });
            }
            return Unauthorized();
        }

        try
        {
            await _questionRepository.Delete(question);

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { 
                    success = true, 
                    message = "Question deleted successfully!",
                    questionId = id
                });
            }

            TempData["SuccessMessage"] = "Question deleted successfully!";
            return RedirectToAction("Details", "Test", new { id = question.TestId });
        }
        catch (Exception ex)
        {
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = false, message = "An error occurred while deleting the question." });
            }
            
            TempData["ErrorMessage"] = "An error occurred while deleting the question.";
            return RedirectToAction("Details", "Test", new { id = question.TestId });
        }
    }
    
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> CreateTrueFalse(string testId)
    {
        var test = await _testRepository.GetTestByIdAsync(testId);
        if (test is null)
        {
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = false, message = "Test not found" });
            }
            return NotFound();
        }
        
        var user = await _userManager.GetUserAsync(User);
        if (user is null || test.UserId != user.Id)
        {
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = false, message = "Unauthorized access" });
            }
            return Unauthorized();
        }
        
        // Check question limit
        var limitCheck = await CheckQuestionLimitAsync(user.Id);
        if (limitCheck != null) return limitCheck;
       
        // now we create the view model
        var model = new CreateTrueFalseQuestionViewModel()
        {
            TestId = testId,
            Points = 1,
            Text = "",
            CorrectAnswer = true
        };

        // For AJAX requests, return JSON data
        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            return Json(new { success = true, data = model });
        }

        return View(model);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreateTrueFalse(CreateTrueFalseQuestionViewModel model)
    {
        if (!ModelState.IsValid)
        {
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                var errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .Select(x => new { Field = x.Key, Message = x.Value.Errors.First().ErrorMessage })
                    .ToArray();
                
                return Json(new { success = false, errors = errors });
            }
            return View(model);
        }
        
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = false, message = "User not authenticated" });
            }
            return Unauthorized();
        }
        
        // Check question limit
        var limitCheck = await CheckQuestionLimitAsync(user.Id);
        if (limitCheck != null) return limitCheck;
        
        var test = await _testRepository.GetTestByIdAsync(model.TestId);
        if (test is null)
        {
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = false, message = "Test not found" });
            }
            return NotFound();
        }
        
        if (test.UserId != user.Id)
        {
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = false, message = "Unauthorized access" });
            }
            return Unauthorized();
        }

        try
        {
            var question = new TrueFalseQuestion()
            {
                TestId = model.TestId,
                Points = model.Points,
                Text = model.Text,
                Position = test.Questions.Count,
                CorrectAnswer = model.CorrectAnswer,
                Test = test
            };
            
            await _questionRepository.Create(question);
            await _subscriptionRepository.IncrementQuestionCountAsync(user.Id);

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { 
                    success = true, 
                    message = "True/False question created successfully!",
                    questionId = question.Id,
                    redirectUrl = Url.Action("Details", "Test", new { id = test.Id })
                });
            }

            TempData["SuccessMessage"] = "True/False question created successfully!";
            return RedirectToAction("Details", "Test", new { id = test.Id });
        }
        catch (Exception ex)
        {
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = false, message = "An error occurred while creating the question." });
            }

            ModelState.AddModelError("", "An error occurred while creating the question.");
            return View(model);
        }
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> CreateMultipleChoice(string testId)
    {
        var test = await _testRepository.GetTestByIdAsync(testId);
        if (test is null)
        {
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = false, message = "Test not found" });
            }
            return NotFound("Test not found");
        }
        
        var user = await _userManager.GetUserAsync(User);
        if (user is null || test.UserId != user.Id)
        {
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = false, message = "Unauthorized access" });
            }
            return Unauthorized();
        }
        
        // Check question limit
        var limitCheck = await CheckQuestionLimitAsync(user.Id);
        if (limitCheck != null) return limitCheck;
        
        var model = new CreateMultipleChoiceQuestionViewModel()
        {
            TestId = testId,
            Points = 1,
            Text = "",
            Options = new List<string>(),
            AllowMultipleSelections = false,
            CorrectAnswers = new List<string>(),
        };

        // For AJAX requests, return JSON data
        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            return Json(new { success = true, data = model });
        }
        
        return View(model);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreateMultipleChoice(CreateMultipleChoiceQuestionViewModel model)
    {
        if(!ModelState.IsValid)
        {
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                var errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .Select(x => new { Field = x.Key, Message = x.Value.Errors.First().ErrorMessage })
                    .ToArray();
                
                return Json(new { success = false, errors = errors });
            }
            return View(model);
        }
        
        var test = await _testRepository.GetTestByIdAsync(model.TestId);
        var user = await _userManager.GetUserAsync(User);
        if (test is null)
        {
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = false, message = "Test not found" });
            }
            return NotFound("test not found");
        }
        if (user is null || test.UserId != user.Id)
        {
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = false, message = "Unauthorized access" });
            }
            return Unauthorized();
        }
        
        // Check question limit
        var limitCheck = await CheckQuestionLimitAsync(user.Id);
        if (limitCheck != null) return limitCheck;

        try
        {
            var question = new MultipleChoiceQuestion()
            {
                TestId = model.TestId,
                Points = model.Points,
                Text = model.Text,
                Position = test.Questions.Count,
                Options = model.Options,
                CorrectAnswers = model.CorrectAnswers,
                AllowMultipleSelections = model.AllowMultipleSelections,
                Test = test,
            };
            
            await _questionRepository.Create(question);
            await _subscriptionRepository.IncrementQuestionCountAsync(user.Id);

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { 
                    success = true, 
                    message = "Multiple choice question created successfully!",
                    questionId = question.Id,
                    redirectUrl = Url.Action("Details", "Test", new { id = test.Id })
                });
            }

            TempData["SuccessMessage"] = "Multiple choice question created successfully!";
            return RedirectToAction("Details", "Test", new { id = test.Id });
        }
        catch (Exception ex)
        {
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = false, message = "An error occurred while creating the question." });
            }

            ModelState.AddModelError("", "An error occurred while creating the question.");
            return View(model);
        }
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> CreateShortAnswer(string testId)
    {
        var test = await _testRepository.GetTestByIdAsync(testId);
        if (test is null)
        {
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = false, message = "Test not found" });
            }
            return NotFound("Test not found");
        }
        
        var user = await _userManager.GetUserAsync(User);
        if (user is null || test.UserId != user.Id)
        {
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = false, message = "Unauthorized access" });
            }
            return Unauthorized();
        }
        
        // Check question limit
        var limitCheck = await CheckQuestionLimitAsync(user.Id);
        if (limitCheck != null) return limitCheck;

        var model = new CreateShortAnswerQuestionViewModel()
        {
            TestId = testId,
            Points = 1,
            Text = "",
            ExpectedAnswer = "",
            CaseSensitive = false,
        };

        // For AJAX requests, return JSON data
        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            return Json(new { success = true, data = model });
        }
        
        return View(model);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreateShortAnswer(CreateShortAnswerQuestionViewModel model)
    {
        if (!ModelState.IsValid)
        {
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                var errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .Select(x => new { Field = x.Key, Message = x.Value.Errors.First().ErrorMessage })
                    .ToArray();
                
                return Json(new { success = false, errors = errors });
            }
            return View(model);
        }

        var test = await _testRepository.GetTestByIdAsync(model.TestId);
        var user = await _userManager.GetUserAsync(User);
        if (test is null)
        {
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = false, message = "Test not found" });
            }
            return NotFound("Test not found");
        }
        if (user is null || test.UserId != user.Id)
        {
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = false, message = "Unauthorized access" });
            }
            return Unauthorized();
        }
        
        // Check question limit
        var limitCheck = await CheckQuestionLimitAsync(user.Id);
        if (limitCheck != null) return limitCheck;

        try
        {
            var question = new ShortAnswerQuestion()
            {
                TestId = model.TestId,
                Points = model.Points,
                Text = model.Text,
                Position = test.Questions.Count,
                ExpectedAnswer = model.ExpectedAnswer,
                CaseSensitive = model.CaseSensitive,
                Test = test,
            };
            
            await _questionRepository.Create(question);
            await _subscriptionRepository.IncrementQuestionCountAsync(user.Id);

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { 
                    success = true, 
                    message = "Short answer question created successfully!",
                    questionId = question.Id,
                    redirectUrl = Url.Action("Details", "Test", new { id = test.Id })
                });
            }

            TempData["SuccessMessage"] = "Short answer question created successfully!";
            return RedirectToAction("Details", "Test", new { id = test.Id });
        }
        catch (Exception ex)
        {
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = false, message = "An error occurred while creating the question." });
            }

            ModelState.AddModelError("", "An error occurred while creating the question.");
            return View(model);
        }
    }
}