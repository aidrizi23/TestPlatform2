using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;
using TestPlatform2.Data;
using TestPlatform2.Models;
using TestPlatform2.Repository;

namespace TestPlatform2.Controllers;

public class TestController : Controller
{
    private readonly ITestRepository _testRepository;
    private readonly UserManager<User> _userManager;
    private readonly ITestAttemptRepository _testAttemptRepository;
    private readonly ITestAnalyticsRepository _testAnalyticsRepository;
    
    public TestController(
        ITestRepository testRepository, 
        UserManager<User> userManager, 
        ITestAttemptRepository testAttemptRepository,
        ITestAnalyticsRepository testAnalyticsRepository)
    {
        _testRepository = testRepository;
        _userManager = userManager;
        _testAttemptRepository = testAttemptRepository;
        _testAnalyticsRepository = testAnalyticsRepository;
    }
    
    // GET: /Test/Index
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        var tests = await _testRepository.GetTestsByUserIdAsync(user.Id);
        return View(tests);
    }
    
    // now the create method for the test
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Create()
    {
        // this will be the default view data for the creation of the test 
        var dto = new TestForCreationDto();
        return View(dto);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create(TestForCreationDto dto)
    {
        if (!ModelState.IsValid)
        {
            // For AJAX requests, return JSON with validation errors
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                var errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .Select(x => new { Field = x.Key, Message = x.Value.Errors.First().ErrorMessage })
                    .ToArray();
                
                return Json(new { success = false, errors = errors });
            }
            
            // If validation fails, return the view with the DTO to show validation errors
            return View(dto);
        }
        
        // Get the current authenticated user
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = false, message = "User not authenticated" });
            }
            // If the user is not found, redirect to the login page
            return RedirectToAction("Login", "Account");
        }
        
        // Map the DTO to the Test entity
        var test = new Test
        {
            TestName = dto.Title,
            Description = dto.Description,
            RandomizeQuestions = dto.RandomizeQuestions,
            TimeLimit = dto.TimeLimit,
            MaxAttempts = dto.MaxAttempts,
            UserId = user.Id // Set the UserId server-side (do not trust client input)
        };

        try
        {
            // Save the test to the database
            await _testRepository.Create(test);

            // For AJAX requests, return JSON success response
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { 
                    success = true, 
                    message = "Test created successfully!",
                    testId = test.Id,
                    redirectUrl = Url.Action("Index")
                });
            }

            // Set a success message to display on the Index page
            TempData["SuccessMessage"] = "Test created successfully!";

            // Redirect to the Index action to show the list of tests
            return RedirectToAction("Index");
        }
        catch (Exception ex)
        {
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = false, message = "An error occurred while creating the test." });
            }

            ModelState.AddModelError("", "An error occurred while creating the test.");
            return View(dto);
        }
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Details(string id)
    {
        // get the user 
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return RedirectToAction("Login", "Account");
        }
        
        // get the test by id
        Test? test = await _testRepository.GetTestByIdAsync(id);

        if (test is null)
        {
            return NotFound();
        }
        
        if(test.User !=  user)
        {
            return Forbid();
        }
        
        return View(test);
    }
    
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Edit(string id)
    {
        // get the user 
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = false, message = "User not authenticated" });
            }
            return RedirectToAction("Login", "Account");
        }
        
        // get the test by id
        Test? test = await _testRepository.GetTestByIdAsync(id);

        if (test is null)
        {
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = false, message = "Test not found" });
            }
            return NotFound();
        }
        
        if(test.User !=  user)
        {
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = false, message = "Unauthorized access" });
            }
            return Forbid();
        }
        
        // now i will create a testForEditDto
        var testForEditDto = new TestForEditDto
        {
            Id = test.Id,
            TestName = test.TestName,
            Description = test.Description,
            RandomizeQuestions = test.RandomizeQuestions,
            TimeLimit = test.TimeLimit,
            MaxAttempts = test.MaxAttempts,
            IsLocked = test.IsLocked,
        };

        // For AJAX requests, return JSON data
        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            return Json(new { success = true, data = testForEditDto });
        }
        
        return View(testForEditDto);
    }
    
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Edit(TestForEditDto dto)
    {
        if (!ModelState.IsValid)
        {
            // For AJAX requests, return JSON with validation errors
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                var errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .Select(x => new { Field = x.Key, Message = x.Value.Errors.First().ErrorMessage })
                    .ToArray();
                
                return Json(new { success = false, errors = errors });
            }
            
            // If validation fails, return the view with the DTO to show validation errors
            return View(dto);
        }
        
        // get the user 
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            if (Request.Headers["X-Requested-With"] == "XMLHttpResponse")
            {
                return Json(new { success = false, message = "User not authenticated" });
            }
            return RedirectToAction("Login", "Account");
        }
        
        // get the test by id
        Test? test = await _testRepository.GetTestByIdAsync(dto.Id);

        if (test is null)
        {
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = false, message = "Test not found" });
            }
            return NotFound();
        }
        
        if(test.User !=  user)
        {
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = false, message = "Unauthorized access" });
            }
            return Forbid();
        }

        try
        {
            // Map the DTO to the Test entity
            test.TestName = dto.TestName;
            test.Description = dto.Description;
            test.RandomizeQuestions = dto.RandomizeQuestions;
            test.TimeLimit = dto.TimeLimit;
            test.MaxAttempts = dto.MaxAttempts;
            test.IsLocked = dto.IsLocked;
            
            // Save the test to the database
            await _testRepository.Update(test);

            // For AJAX requests, return JSON success response
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { 
                    success = true, 
                    message = "Test updated successfully!",
                    redirectUrl = Url.Action("Index")
                });
            }

            // Set a success message to display on the Index page
            TempData["SuccessMessage"] = "Test updated successfully!";

            // Redirect to the Index action to show the list of tests
            return RedirectToAction("Index");
        }
        catch (Exception ex)
        {
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = false, message = "An error occurred while updating the test." });
            }

            ModelState.AddModelError("", "An error occurred while updating the test.");
            return View(dto);
        }
    }
    
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Delete(string id)
    {
        // get the user 
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = false, message = "User not authenticated" });
            }
            return RedirectToAction("Login", "Account");
        }
        
        // get the test by id
        Test? test = await _testRepository.GetTestByIdAsync(id);

        if (test is null)
        {
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = false, message = "Test not found" });
            }
            return NotFound();
        }
        
        if(test.User !=  user)
        {
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = false, message = "Unauthorized access" });
            }
            return Forbid();
        }

        try
        {
            // delete the test
            await _testRepository.Delete(test);

            // For AJAX requests, return JSON success response
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { 
                    success = true, 
                    message = "Test deleted successfully!"
                });
            }

            // Set a success message to display on the Index page
            TempData["SuccessMessage"] = "Test deleted successfully!";

            // Redirect to the Index action to show the list of tests
            return RedirectToAction("Index");
        }
        catch (Exception ex)
        {
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = false, message = "An error occurred while deleting the test." });
            }

            TempData["ErrorMessage"] = "An error occurred while deleting the test.";
            return RedirectToAction("Index");
        }
    }
    
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> AllAttempts(string testId, string filter = "All") // Added filter parameter
    {
        // get and authenticate the user
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
            return RedirectToAction("Login", "Account");

        // get the test by id
        var test = await _testRepository.GetTestByIdAsync(testId);

        if (test is null)
            return NotFound();
        if (test.User != user)
            return Unauthorized();

        // get all the submissions of the test
        var allAttempts = await _testAttemptRepository.GetAttemptsByTestIdAsync(testId);
        var finishedAttempts = await _testAttemptRepository.GetFinishedAttemptsByTestIdAsync(testId);
        var unfinishedAttempts = await _testAttemptRepository.GetUnfinishedAttemptsByTestIdAsync(testId);

        var viewModel = new TestAttemptsViewModel
        {
            TestId = testId,
            AllAttempts = allAttempts,
            FinishedAttempts = finishedAttempts ?? Enumerable.Empty<TestAttempt>(), //Handle null
            UnfinishedAttempts = unfinishedAttempts ?? Enumerable.Empty<TestAttempt>(), //Handle null
            CurrentFilter = filter // Set the current filter
        };

        return View(viewModel);
    }
    
    [HttpPost]
    public async Task<IActionResult> LockTest(string id)
    {
        var test = await _testRepository.GetTestByIdAsync(id);
        if (test == null)
        {
            return NotFound();
        }

        var user = await _userManager.GetUserAsync(User);
        if (user!.Id != test.UserId)
        {
            return Unauthorized();
        }

        test.IsLocked = !test.IsLocked;
        await _testRepository.Update(test);

        return Json(new { success = true, isLocked = test.IsLocked });
    }
    
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Analytics(string id)
    {
        // Get and authenticate the user
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
            return RedirectToAction("Login", "Account");

        // Get the test by id
        var test = await _testRepository.GetTestByIdAsync(id);
        if (test is null)
            return NotFound("Test not found");
                
        // Verify the user owns this test
        if (test.User != user)
            return Unauthorized("You do not have permission to view analytics for this test");

        // Get the analytics data
        var analyticsData = await _testAnalyticsRepository.GetTestAnalyticsAsync(id);
        if (analyticsData is null)
            return NotFound("Could not generate analytics for this test");

        return View(analyticsData);
    }
    
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> QuestionAnalytics(string testId, string questionId)
    {
        // Get and authenticate the user
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
            return RedirectToAction("Login", "Account");

        // Get the test by id
        var test = await _testRepository.GetTestByIdAsync(testId);
        if (test is null)
            return NotFound("Test not found");
        
        // Verify the user owns this test
        if (test.User != user)
            return Unauthorized("You do not have permission to view analytics for this test");
    
        // Get the question
        var question = test.Questions.FirstOrDefault(q => q.Id == questionId);
        if (question is null)
            return NotFound("Question not found");
    
        // Get the question analytics data
        var questionAnalytics = (await _testAnalyticsRepository.GetQuestionPerformanceDataAsync(testId))
            .FirstOrDefault(q => q.QuestionId == questionId);
    
        if (questionAnalytics is null)
            return NotFound("Could not generate analytics for this question");
    
        // Pass both the question and its analytics to the view
        var viewModel = new QuestionAnalyticsViewModel
        {
            TestId = testId,
            TestName = test.TestName,
            Question = question,
            Analytics = questionAnalytics
        };
    
        return View(viewModel);
    }
}