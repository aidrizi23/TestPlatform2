using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using TestPlatform2.Data;
using TestPlatform2.Data.Questions;
using TestPlatform2.Repository;

namespace TestPlatform2.Controllers;

public class QuestionController : Controller
{
    private readonly IQuestionRepository _questionRepository;
    private readonly UserManager<User> _userManager;
    private readonly ITestRepository _testRepository;
    
    public QuestionController(IQuestionRepository questionRepository, UserManager<User> userManager, ITestRepository testRepository)
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
    
    
    // now for the creation of the tests
    
    // firstly i will create the truefalse question 
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
        
        // get the current user
        var user = await _userManager.GetUserAsync(User);

        if (user is null)
        {
            return  Unauthorized();
        }
        
        // get the test
        var test = await _testRepository.GetTestByIdAsync(model.TestId);
        if (test is null)
        {
            return NotFound();
        }
        
        // now create the question
        var question = new TrueFalseQuestion()
        {
            TestId = model.TestId,
            Points = model.Points,
            Text = model.Text,
            Position = test.Questions.Count, // will start from 0 and increment by 1
            CorrectAnswer = model.CorrectAnswer,
            Test = test!
        };
        
        await _questionRepository.Create(question);
        return RedirectToAction("Details", "Test", new { id = test.Id });
        
        
    }
    
    
    
}