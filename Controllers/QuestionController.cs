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
    
    public QuestionController(
        IQuestionRepository questionRepository, 
        UserManager<User> userManager, 
        ITestRepository testRepository)
    {
        _questionRepository = questionRepository;
        _userManager = userManager;
        _testRepository = testRepository;
    }
    
   
    
    // method to delete question
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Delete(string id)
    {
        var question = await _questionRepository.GetQuestionByIdAsync(id);
        if (question is null)
        {
            return NotFound();
        }
        
        var test = await _testRepository.GetTestByIdAsync(question.TestId);
        if (test is null)
        {
            return NotFound();
        }
        
        var user = await _userManager.GetUserAsync(User);
        if (user is null || test.UserId != user.Id)
        {
            return Unauthorized();
        }
        
        await _questionRepository.Delete(question);
        return RedirectToAction("Details", "Test", new { id = question.TestId });
    }
    
    
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> CreateTrueFalse(string testId)
    {
        var test = await _testRepository.GetTestByIdAsync(testId);
        if (test is null)
        {
            return NotFound();
        }
        
        var user = await _userManager.GetUserAsync(User);
        if (user is null || test.UserId != user.Id)
        {
            return Unauthorized();
        }
        
       
       
        // now we create the view model
        var model = new CreateTrueFalseQuestionViewModel()
        {
            TestId = testId,
            Points = 1,
            Text = "",
            CorrectAnswer = true
        };
        return View(model);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreateTrueFalse(CreateTrueFalseQuestionViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);
        
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Unauthorized();
        }
        
       
        
        var test = await _testRepository.GetTestByIdAsync(model.TestId);
        if (test is null)
        {
            return NotFound();
        }
        
        if (test.UserId != user.Id)
        {
            return Unauthorized();
        }
        
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
       
        
        return RedirectToAction("Details", "Test", new { id = test.Id });
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> CreateMultipleChoice(string testId)
    {
        var test = await _testRepository.GetTestByIdAsync(testId);
        if (test is null)
        {
            return NotFound("Test not found");
        }
        
        var user = await _userManager.GetUserAsync(User);
        if (user is null || test.UserId != user.Id)
        {
            return Unauthorized();
        }
        
        // Check question limit
     
       
        
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
       
        
        return RedirectToAction("Details", "Test", new { id = test.Id });
    }

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

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreateShortAnswer(CreateShortAnswerQuestionViewModel model)
    {
        var test = await _testRepository.GetTestByIdAsync(model.TestId);
        var user = await _userManager.GetUserAsync(User);
        if (test is null)
            return NotFound("Test not found");
        if (user is null || test.UserId != user!.Id)
            return Unauthorized();
        

        var question = new ShortAnswerQuestion()
        {
            TestId = model.TestId,
            Points = model.Points,
            Text = model.Text,
            Position = test.Questions.Count,
            ExpectedAnswer = model.ExpectedAnswer,
            CaseSensitive = model.CaseSensitive,
            Test = test!,
        };
        
        await _questionRepository.Create(question);
        
        return RedirectToAction("Details", "Test", new { id = test.Id });
    }
}