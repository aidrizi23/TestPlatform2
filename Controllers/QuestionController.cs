using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TestPlatform2.Data;
using TestPlatform2.Data.Questions;
using TestPlatform2.Repository;
using TestPlatform2.Models.Questions;

namespace TestPlatform2.Controllers;

public class QuestionController : Controller
{
    private readonly IQuestionRepository _questionRepository;
    private readonly UserManager<User> _userManager;
    private readonly ITestRepository _testRepository;
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly ITestAttemptRepository _testAttemptRepository;
    
    public QuestionController(
        IQuestionRepository questionRepository, 
        UserManager<User> userManager, 
        ITestRepository testRepository,
        ISubscriptionRepository subscriptionRepository,
        ITestAttemptRepository testAttemptRepository)
    {
        _questionRepository = questionRepository;
        _userManager = userManager;
        _testRepository = testRepository;
        _subscriptionRepository = subscriptionRepository;
        _testAttemptRepository = testAttemptRepository;
    }
    
    private async Task<IActionResult> CheckQuestionLimitAsync(string userId)
    {
        if (!await _subscriptionRepository.CanCreateQuestionAsync(userId))
        {
            TempData["ErrorMessage"] = "You've reached your free question limit (30 questions). Upgrade to Pro for unlimited questions!";
            return RedirectToAction("Index", "Subscription");
        }
        return null;
    }
    
    private async Task<bool> TestHasAttemptsAsync(string testId)
    {
        var attempts = await _testAttemptRepository.GetAttemptsByTestIdAsync(testId);
        return attempts.Any(a => a.IsCompleted);
    }

    // ========== TRADITIONAL MVC METHODS ==========
    
    // GET: /Question/CreateTrueFalse
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> CreateTrueFalse(string testId)
    {
        var test = await _testRepository.GetTestByIdAsync(testId);
        if (test is null)
            return NotFound();
        
        var user = await _userManager.GetUserAsync(User);
        if (user is null || test.UserId != user.Id)
            return Unauthorized();
        
        // Check question limit
        var limitCheck = await CheckQuestionLimitAsync(user.Id);
        if (limitCheck != null) return limitCheck;
       
        var model = new CreateTrueFalseQuestionViewModel()
        {
            TestId = testId,
            Points = 1,
            Text = "",
            CorrectAnswer = true
        };

        return View(model);
    }

    // POST: /Question/CreateTrueFalse
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreateTrueFalse(CreateTrueFalseQuestionViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);
        
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
            return Unauthorized();
        
        // Check question limit
        var limitCheck = await CheckQuestionLimitAsync(user.Id);
        if (limitCheck != null) return limitCheck;
        
        var test = await _testRepository.GetTestByIdAsync(model.TestId);
        if (test is null)
            return NotFound();
        
        if (test.UserId != user.Id)
            return Unauthorized();

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

            TempData["SuccessMessage"] = "True/False question created successfully!";
            return RedirectToAction("Details", "Test", new { id = test.Id });
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", "An error occurred while creating the question.");
            return View(model);
        }
    }

    // GET: /Question/CreateMultipleChoice
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> CreateMultipleChoice(string testId)
    {
        var test = await _testRepository.GetTestByIdAsync(testId);
        if (test is null)
            return NotFound("Test not found");
        
        var user = await _userManager.GetUserAsync(User);
        if (user is null || test.UserId != user.Id)
            return Unauthorized();
        
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
        
        return View(model);
    }

    // POST: /Question/CreateMultipleChoice
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreateMultipleChoice(CreateMultipleChoiceQuestionViewModel model)
    {
        if(!ModelState.IsValid)
            return View(model);
        
        var test = await _testRepository.GetTestByIdAsync(model.TestId);
        var user = await _userManager.GetUserAsync(User);
        if (test is null)
            return NotFound("test not found");
        if (user is null || test.UserId != user.Id)
            return Unauthorized();
        
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

            TempData["SuccessMessage"] = "Multiple choice question created successfully!";
            return RedirectToAction("Details", "Test", new { id = test.Id });
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", "An error occurred while creating the question.");
            return View(model);
        }
    }

    // GET: /Question/CreateShortAnswer
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> CreateShortAnswer(string testId)
    {
        var test = await _testRepository.GetTestByIdAsync(testId);
        if (test is null)
            return NotFound("Test not found");
        
        var user = await _userManager.GetUserAsync(User);
        if (user is null || test.UserId != user.Id)
            return Unauthorized();
        
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
        
        return View(model);
    }

    // POST: /Question/CreateShortAnswer
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreateShortAnswer(CreateShortAnswerQuestionViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var test = await _testRepository.GetTestByIdAsync(model.TestId);
        var user = await _userManager.GetUserAsync(User);
        if (test is null)
            return NotFound("Test not found");
        if (user is null || test.UserId != user.Id)
            return Unauthorized();
        
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

            TempData["SuccessMessage"] = "Short answer question created successfully!";
            return RedirectToAction("Details", "Test", new { id = test.Id });
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", "An error occurred while creating the question.");
            return View(model);
        }
    }

    // POST: /Question/Delete
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Delete(string id)
    {
        var question = await _questionRepository.GetQuestionByIdAsync(id);
        if (question is null)
            return NotFound();
        
        var test = await _testRepository.GetTestByIdAsync(question.TestId);
        if (test is null)
            return NotFound();
        
        var user = await _userManager.GetUserAsync(User);
        if (user is null || test.UserId != user.Id)
            return Unauthorized();

        // Check if test has completed attempts
        if (await TestHasAttemptsAsync(question.TestId))
        {
            TempData["ErrorMessage"] = "Cannot delete questions from a test that has completed attempts. This preserves the integrity of student submissions.";
            return RedirectToAction("Details", "Test", new { id = question.TestId });
        }

        try
        {
            await _questionRepository.Delete(question);
            TempData["SuccessMessage"] = "Question deleted successfully!";
            return RedirectToAction("Details", "Test", new { id = question.TestId });
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = "An error occurred while deleting the question.";
            return RedirectToAction("Details", "Test", new { id = question.TestId });
        }
    }

    // GET: /Question/CreateDragDrop
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> CreateDragDrop(string testId)
    {
        var test = await _testRepository.GetTestByIdAsync(testId);
        if (test is null)
            return NotFound("Test not found");
        
        var user = await _userManager.GetUserAsync(User);
        if (user is null || test.UserId != user.Id)
            return Unauthorized();
        
        // Check question limit
        var limitCheck = await CheckQuestionLimitAsync(user.Id);
        if (limitCheck != null) return limitCheck;

        var model = new CreateDragDropQuestionViewModel()
        {
            TestId = testId,
            Points = 1,
            Text = "",
            AllowMultiplePerZone = false,
            OrderMatters = false,
            DraggableItemsJson = "[]",
            DropZonesJson = "[]"
        };
        
        return View(model);
    }

    // POST: /Question/CreateDragDrop
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreateDragDrop(CreateDragDropQuestionViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var test = await _testRepository.GetTestByIdAsync(model.TestId);
        var user = await _userManager.GetUserAsync(User);
        if (test is null)
            return NotFound("Test not found");
        if (user is null || test.UserId != user.Id)
            return Unauthorized();
        
        // Check question limit
        var limitCheck = await CheckQuestionLimitAsync(user.Id);
        if (limitCheck != null) return limitCheck;

        try
        {
            var question = new DragDropQuestion()
            {
                TestId = model.TestId,
                Points = model.Points,
                Text = model.Text,
                Position = test.Questions.Count,
                AllowMultiplePerZone = model.AllowMultiplePerZone,
                OrderMatters = model.OrderMatters,
                DraggableItemsJson = model.DraggableItemsJson,
                DropZonesJson = model.DropZonesJson,
                Test = test,
            };
            
            await _questionRepository.Create(question);
            await _subscriptionRepository.IncrementQuestionCountAsync(user.Id);

            TempData["SuccessMessage"] = "Drag & drop question created successfully!";
            return RedirectToAction("Details", "Test", new { id = test.Id });
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", "An error occurred while creating the question.");
            return View(model);
        }
    }

    // GET: /Question/CreateImageBased
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> CreateImageBased(string testId)
    {
        var test = await _testRepository.GetTestByIdAsync(testId);
        if (test is null)
            return NotFound("Test not found");
        
        var user = await _userManager.GetUserAsync(User);
        if (user is null || test.UserId != user.Id)
            return Unauthorized();
        
        // Check question limit
        var limitCheck = await CheckQuestionLimitAsync(user.Id);
        if (limitCheck != null) return limitCheck;

        var model = new CreateImageBasedQuestionViewModel()
        {
            TestId = testId,
            Points = 1,
            Text = "",
            ImageUrl = "",
            QuestionType = ImageQuestionType.Hotspot,
            ImageWidth = 800,
            ImageHeight = 600,
            HotspotsJson = "[]",
            LabelsJson = "[]"
        };
        
        return View(model);
    }

    // POST: /Question/CreateImageBased
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreateImageBased(CreateImageBasedQuestionViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var test = await _testRepository.GetTestByIdAsync(model.TestId);
        var user = await _userManager.GetUserAsync(User);
        if (test is null)
            return NotFound("Test not found");
        if (user is null || test.UserId != user.Id)
            return Unauthorized();
        
        // Check question limit
        var limitCheck = await CheckQuestionLimitAsync(user.Id);
        if (limitCheck != null) return limitCheck;

        try
        {
            var question = new ImageBasedQuestion()
            {
                TestId = model.TestId,
                Points = model.Points,
                Text = model.Text,
                Position = test.Questions.Count,
                ImageUrl = model.ImageUrl,
                QuestionType = model.QuestionType,
                AltText = model.AltText,
                ImageWidth = model.ImageWidth,
                ImageHeight = model.ImageHeight,
                HotspotsJson = model.HotspotsJson,
                LabelsJson = model.LabelsJson,
                Test = test,
            };
            
            await _questionRepository.Create(question);
            await _subscriptionRepository.IncrementQuestionCountAsync(user.Id);

            TempData["SuccessMessage"] = "Image-based question created successfully!";
            return RedirectToAction("Details", "Test", new { id = test.Id });
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", "An error occurred while creating the question.");
            return View(model);
        }
    }

    // ========== AJAX METHODS ==========

    // GET: /Question/GetTrueFalseFormDataAjax
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetTrueFalseFormDataAjax(string testId)
    {
        var test = await _testRepository.GetTestByIdAsync(testId);
        if (test is null)
            return Json(new { success = false, message = "Test not found" });
        
        var user = await _userManager.GetUserAsync(User);
        if (user is null || test.UserId != user.Id)
            return Json(new { success = false, message = "Unauthorized access" });
        
        // Check question limit
        if (!await _subscriptionRepository.CanCreateQuestionAsync(user.Id))
            return Json(new { success = false, message = "You've reached your free question limit (30 questions). Upgrade to Pro for unlimited questions!" });
       
        var model = new CreateTrueFalseQuestionViewModel()
        {
            TestId = testId,
            Points = 1,
            Text = "",
            CorrectAnswer = true
        };

        return Json(new { success = true, data = model });
    }

    // POST: /Question/CreateTrueFalseAjax
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreateTrueFalseAjax([FromBody] CreateTrueFalseQuestionViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState
                .Where(x => x.Value.Errors.Count > 0)
                .Select(x => new { Field = x.Key, Message = x.Value.Errors.First().ErrorMessage })
                .ToArray();
            
            return Json(new { success = false, errors = errors });
        }
        
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
            return Json(new { success = false, message = "User not authenticated" });
        
        // Check question limit
        if (!await _subscriptionRepository.CanCreateQuestionAsync(user.Id))
            return Json(new { success = false, message = "You've reached your free question limit (30 questions). Upgrade to Pro for unlimited questions!" });
        
        var test = await _testRepository.GetTestByIdAsync(model.TestId);
        if (test is null)
            return Json(new { success = false, message = "Test not found" });
        
        if (test.UserId != user.Id)
            return Json(new { success = false, message = "Unauthorized access" });

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

            return Json(new { 
                success = true, 
                message = "True/False question created successfully!",
                questionId = question.Id,
                question = new {
                    id = question.Id,
                    text = question.Text,
                    points = question.Points,
                    position = question.Position,
                    type = "TrueFalse",
                    correctAnswer = question.CorrectAnswer
                }
            });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "An error occurred while creating the question." });
        }
    }

    // GET: /Question/GetMultipleChoiceFormDataAjax
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetMultipleChoiceFormDataAjax(string testId)
    {
        var test = await _testRepository.GetTestByIdAsync(testId);
        if (test is null)
            return Json(new { success = false, message = "Test not found" });
        
        var user = await _userManager.GetUserAsync(User);
        if (user is null || test.UserId != user.Id)
            return Json(new { success = false, message = "Unauthorized access" });
        
        // Check question limit
        if (!await _subscriptionRepository.CanCreateQuestionAsync(user.Id))
            return Json(new { success = false, message = "You've reached your free question limit (30 questions). Upgrade to Pro for unlimited questions!" });
        
        var model = new CreateMultipleChoiceQuestionViewModel()
        {
            TestId = testId,
            Points = 1,
            Text = "",
            Options = new List<string> { "", "" }, // Start with 2 empty options
            AllowMultipleSelections = false,
            CorrectAnswers = new List<string>(),
        };

        return Json(new { success = true, data = model });
    }

    // POST: /Question/CreateMultipleChoiceAjax
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreateMultipleChoiceAjax([FromBody] CreateMultipleChoiceQuestionViewModel model)
    {
        if(!ModelState.IsValid)
        {
            var errors = ModelState
                .Where(x => x.Value.Errors.Count > 0)
                .Select(x => new { Field = x.Key, Message = x.Value.Errors.First().ErrorMessage })
                .ToArray();
            
            return Json(new { success = false, errors = errors });
        }
        
        var test = await _testRepository.GetTestByIdAsync(model.TestId);
        var user = await _userManager.GetUserAsync(User);
        if (test is null)
            return Json(new { success = false, message = "Test not found" });
        if (user is null || test.UserId != user.Id)
            return Json(new { success = false, message = "Unauthorized access" });
        
        // Check question limit
        if (!await _subscriptionRepository.CanCreateQuestionAsync(user.Id))
            return Json(new { success = false, message = "You've reached your free question limit (30 questions). Upgrade to Pro for unlimited questions!" });

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

            return Json(new { 
                success = true, 
                message = "Multiple choice question created successfully!",
                questionId = question.Id,
                question = new {
                    id = question.Id,
                    text = question.Text,
                    points = question.Points,
                    position = question.Position,
                    type = "MultipleChoice",
                    options = question.Options,
                    correctAnswers = question.CorrectAnswers,
                    allowMultipleSelections = question.AllowMultipleSelections
                }
            });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "An error occurred while creating the question." });
        }
    }

    // GET: /Question/GetShortAnswerFormDataAjax
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetShortAnswerFormDataAjax(string testId)
    {
        var test = await _testRepository.GetTestByIdAsync(testId);
        if (test is null)
            return Json(new { success = false, message = "Test not found" });
        
        var user = await _userManager.GetUserAsync(User);
        if (user is null || test.UserId != user.Id)
            return Json(new { success = false, message = "Unauthorized access" });
        
        // Check question limit
        if (!await _subscriptionRepository.CanCreateQuestionAsync(user.Id))
            return Json(new { success = false, message = "You've reached your free question limit (30 questions). Upgrade to Pro for unlimited questions!" });

        var model = new CreateShortAnswerQuestionViewModel()
        {
            TestId = testId,
            Points = 1,
            Text = "",
            ExpectedAnswer = "",
            CaseSensitive = false,
        };

        return Json(new { success = true, data = model });
    }

    // POST: /Question/CreateShortAnswerAjax
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreateShortAnswerAjax([FromBody] CreateShortAnswerQuestionViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState
                .Where(x => x.Value.Errors.Count > 0)
                .Select(x => new { Field = x.Key, Message = x.Value.Errors.First().ErrorMessage })
                .ToArray();
            
            return Json(new { success = false, errors = errors });
        }

        var test = await _testRepository.GetTestByIdAsync(model.TestId);
        var user = await _userManager.GetUserAsync(User);
        if (test is null)
            return Json(new { success = false, message = "Test not found" });
        if (user is null || test.UserId != user.Id)
            return Json(new { success = false, message = "Unauthorized access" });
        
        // Check question limit
        if (!await _subscriptionRepository.CanCreateQuestionAsync(user.Id))
            return Json(new { success = false, message = "You've reached your free question limit (30 questions). Upgrade to Pro for unlimited questions!" });

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

            return Json(new { 
                success = true, 
                message = "Short answer question created successfully!",
                questionId = question.Id,
                question = new {
                    id = question.Id,
                    text = question.Text,
                    points = question.Points,
                    position = question.Position,
                    type = "ShortAnswer",
                    expectedAnswer = question.ExpectedAnswer,
                    caseSensitive = question.CaseSensitive
                }
            });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "An error occurred while creating the question." });
        }
    }

    // POST: /Question/DeleteAjax
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> DeleteAjax([FromBody] DeleteQuestionRequest request)
    {
        var question = await _questionRepository.GetQuestionByIdAsync(request.Id);
        if (question is null)
            return Json(new { success = false, message = "Question not found" });
        
        var test = await _testRepository.GetTestByIdAsync(question.TestId);
        if (test is null)
            return Json(new { success = false, message = "Test not found" });
        
        var user = await _userManager.GetUserAsync(User);
        if (user is null || test.UserId != user.Id)
            return Json(new { success = false, message = "Unauthorized access" });

        // Check if test has completed attempts
        if (await TestHasAttemptsAsync(question.TestId))
        {
            return Json(new { success = false, message = "Cannot delete questions from a test that has completed attempts. This preserves the integrity of student submissions." });
        }

        try
        {
            await _questionRepository.Delete(question);

            return Json(new { 
                success = true, 
                message = "Question deleted successfully!",
                questionId = request.Id
            });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "An error occurred while deleting the question." });
        }
    }

    // GET: /Question/GetQuestionDataAjax
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetQuestionDataAjax(string id)
    {
        var question = await _questionRepository.GetQuestionByIdAsync(id);
        if (question is null)
            return Json(new { success = false, message = "Question not found" });
        
        var test = await _testRepository.GetTestByIdAsync(question.TestId);
        if (test is null)
            return Json(new { success = false, message = "Test not found" });
        
        var user = await _userManager.GetUserAsync(User);
        if (user is null || test.UserId != user.Id)
            return Json(new { success = false, message = "Unauthorized access" });

        try
        {
            var questionData = new
            {
                id = question.Id,
                text = question.Text,
                points = question.Points,
                position = question.Position,
                testId = question.TestId,
                type = question.GetType().Name.Replace("Question", "")
            };

            // Add type-specific data
            object typeSpecificData = question switch
            {
                TrueFalseQuestion tfq => new { correctAnswer = tfq.CorrectAnswer },
                MultipleChoiceQuestion mcq => new { 
                    options = mcq.Options, 
                    correctAnswers = mcq.CorrectAnswers,
                    allowMultipleSelections = mcq.AllowMultipleSelections 
                },
                ShortAnswerQuestion saq => new { 
                    expectedAnswer = saq.ExpectedAnswer,
                    caseSensitive = saq.CaseSensitive 
                },
                _ => new { }
            };

            return Json(new { 
                success = true, 
                data = new { 
                    question = questionData,
                    typeSpecific = typeSpecificData
                }
            });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "An error occurred while retrieving the question." });
        }
    }

    // POST: /Question/BulkDeleteQuestionsAjax
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> BulkDeleteQuestionsAjax([FromBody] BulkDeleteQuestionsRequest request)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
            return Json(new { success = false, message = "User not authenticated" });

        if (request.QuestionIds == null || !request.QuestionIds.Any())
            return Json(new { success = false, message = "No questions selected" });

        var deletedCount = 0;
        var errors = new List<string>();

        foreach (var questionId in request.QuestionIds)
        {
            try
            {
                var question = await _questionRepository.GetQuestionByIdAsync(questionId);
                if (question != null)
                {
                    var test = await _testRepository.GetTestByIdAsync(question.TestId);
                    if (test != null && test.UserId == user.Id)
                    {
                        // Check if test has completed attempts
                        if (await TestHasAttemptsAsync(question.TestId))
                        {
                            errors.Add($"Cannot delete question from test with completed attempts");
                            continue;
                        }

                        await _questionRepository.Delete(question);
                        deletedCount++;
                    }
                    else
                    {
                        errors.Add($"Unauthorized to delete question {questionId}");
                    }
                }
                else
                {
                    errors.Add($"Question {questionId} not found");
                }
            }
            catch (Exception ex)
            {
                errors.Add($"Error deleting question {questionId}");
            }
        }

        return Json(new
        {
            success = deletedCount > 0,
            message = $"Deleted {deletedCount} question(s)",
            deletedCount,
            errors
        });
    }

    // GET: /Question/GetQuestionsByTestIdAjax
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetQuestionsByTestIdAjax(string testId)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
            return Json(new { success = false, message = "User not authenticated" });

        var test = await _testRepository.GetTestByIdAsync(testId);
        if (test is null)
            return Json(new { success = false, message = "Test not found" });

        if (test.UserId != user.Id)
            return Json(new { success = false, message = "Unauthorized access" });

        try
        {
            var questions = test.Questions.OrderBy(q => q.Position).Select(q => new
            {
                id = q.Id,
                text = q.Text,
                points = q.Points,
                position = q.Position,
                type = q.GetType().Name.Replace("Question", "")
            });

            return Json(new { success = true, data = questions });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Error retrieving questions" });
        }
    }

    // ========== REQUEST DTOs FOR AJAX METHODS ==========
    
    public class DeleteQuestionRequest
    {
        public string Id { get; set; }
    }

    public class BulkDeleteQuestionsRequest
    {
        public List<string> QuestionIds { get; set; }
    }
}