using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TestPlatform2.Data;
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
    public async Task<IActionResult> Delete(string questionId)
    {
        var question = await _questionRepository.GetQuestionByIdAsync(questionId);
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
        return RedirectToAction("Details", "Test", new { testId = question.TestId });
    }
    
    
    
    
}