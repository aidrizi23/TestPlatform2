using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;
using TestPlatform2.Data;
using TestPlatform2.Models;
using TestPlatform2.Repository;
using System.Text.Json;

namespace TestPlatform2.Controllers;

public class TestController : Controller
{
    private readonly ITestRepository _testRepository;
    private readonly UserManager<User> _userManager;
    private readonly ITestAttemptRepository _testAttemptRepository;
    private readonly ITestAnalyticsRepository _testAnalyticsRepository;
    private readonly ITestInviteRepository _testInviteRepository;
    
    public TestController(
        ITestRepository testRepository, 
        UserManager<User> userManager, 
        ITestAttemptRepository testAttemptRepository,
        ITestAnalyticsRepository testAnalyticsRepository,
        ITestInviteRepository testInviteRepository)
    {
        _testRepository = testRepository;
        _userManager = userManager;
        _testAttemptRepository = testAttemptRepository;
        _testAnalyticsRepository = testAnalyticsRepository;
        _testInviteRepository = testInviteRepository;
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

    // ========== AJAX-SPECIFIC METHODS ==========

    /// <summary>
    /// Gets test data for modal editing
    /// </summary>
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetTestDataForModal(string id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
            return Json(new { success = false, message = "User not authenticated" });

        var test = await _testRepository.GetTestByIdAsync(id);
        if (test is null)
            return Json(new { success = false, message = "Test not found" });

        if (test.User != user)
            return Json(new { success = false, message = "Unauthorized access" });

        var testData = new
        {
            id = test.Id,
            testName = test.TestName,
            description = test.Description,
            randomizeQuestions = test.RandomizeQuestions,
            timeLimit = test.TimeLimit,
            maxAttempts = test.MaxAttempts,
            isLocked = test.IsLocked,
            questionCount = test.Questions.Count,
            attemptCount = test.Attempts.Count,
            inviteCount = test.InvitedStudents.Count
        };

        return Json(new { success = true, data = testData });
    }

    /// <summary>
    /// Quick update for specific test properties
    /// </summary>
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> QuickUpdateTest([FromBody] QuickUpdateTestDto dto)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
            return Json(new { success = false, message = "User not authenticated" });

        var test = await _testRepository.GetTestByIdAsync(dto.TestId);
        if (test is null)
            return Json(new { success = false, message = "Test not found" });

        if (test.User != user)
            return Json(new { success = false, message = "Unauthorized access" });

        try
        {
            switch (dto.Property.ToLower())
            {
                case "testname":
                    test.TestName = dto.Value?.ToString() ?? test.TestName;
                    break;
                case "description":
                    test.Description = dto.Value?.ToString() ?? test.Description;
                    break;
                case "timelimit":
                    if (int.TryParse(dto.Value?.ToString(), out int timeLimit))
                        test.TimeLimit = timeLimit;
                    break;
                case "maxattempts":
                    if (int.TryParse(dto.Value?.ToString(), out int maxAttempts))
                        test.MaxAttempts = maxAttempts;
                    break;
                case "randomizequestions":
                    if (bool.TryParse(dto.Value?.ToString(), out bool randomize))
                        test.RandomizeQuestions = randomize;
                    break;
                case "islocked":
                    if (bool.TryParse(dto.Value?.ToString(), out bool locked))
                        test.IsLocked = locked;
                    break;
                default:
                    return Json(new { success = false, message = "Invalid property" });
            }

            await _testRepository.Update(test);
            return Json(new { success = true, message = "Test updated successfully" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Error updating test" });
        }
    }

    /// <summary>
    /// Gets quick statistics for a test
    /// </summary>
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetTestStatistics(string id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
            return Json(new { success = false, message = "User not authenticated" });

        var test = await _testRepository.GetTestByIdAsync(id);
        if (test is null)
            return Json(new { success = false, message = "Test not found" });

        if (test.User != user)
            return Json(new { success = false, message = "Unauthorized access" });

        var stats = await _testRepository.GetTestAnalyticsSummaryAsync(id);
        if (stats == null)
            return Json(new { success = false, message = "Could not retrieve statistics" });

        return Json(new { success = true, data = stats });
    }

    /// <summary>
    /// Gets test questions list for dynamic loading
    /// </summary>
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetTestQuestions(string id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
            return Json(new { success = false, message = "User not authenticated" });

        var test = await _testRepository.GetTestByIdAsync(id);
        if (test is null)
            return Json(new { success = false, message = "Test not found" });

        if (test.User != user)
            return Json(new { success = false, message = "Unauthorized access" });

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

    /// <summary>
    /// Bulk delete multiple tests
    /// </summary>
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> BulkDeleteTests([FromBody] List<string> testIds)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
            return Json(new { success = false, message = "User not authenticated" });

        if (testIds == null || !testIds.Any())
            return Json(new { success = false, message = "No tests selected" });

        var deletedCount = 0;
        var errors = new List<string>();

        foreach (var testId in testIds)
        {
            try
            {
                var test = await _testRepository.GetTestByIdAsync(testId);
                if (test != null && test.User == user)
                {
                    await _testRepository.Delete(test);
                    deletedCount++;
                }
                else
                {
                    errors.Add($"Could not delete test {testId}");
                }
            }
            catch (Exception ex)
            {
                errors.Add($"Error deleting test {testId}");
            }
        }

        return Json(new
        {
            success = deletedCount > 0,
            message = $"Deleted {deletedCount} test(s)",
            deletedCount,
            errors
        });
    }

    /// <summary>
    /// Clones an existing test
    /// </summary>
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CloneTest(string id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
            return Json(new { success = false, message = "User not authenticated" });

        var originalTest = await _testRepository.GetTestByIdAsync(id);
        if (originalTest is null)
            return Json(new { success = false, message = "Test not found" });

        if (originalTest.User != user)
            return Json(new { success = false, message = "Unauthorized access" });

        try
        {
            var clonedTest = new Test
            {
                TestName = $"{originalTest.TestName} (Copy)",
                Description = originalTest.Description,
                RandomizeQuestions = originalTest.RandomizeQuestions,
                TimeLimit = originalTest.TimeLimit,
                MaxAttempts = originalTest.MaxAttempts,
                UserId = user.Id,
                IsLocked = false // New test starts unlocked
            };

            await _testRepository.Create(clonedTest);

            return Json(new
            {
                success = true,
                message = "Test cloned successfully",
                newTestId = clonedTest.Id,
                newTestName = clonedTest.TestName
            });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Error cloning test" });
        }
    }

    /// <summary>
    /// Gets recent activity for all tests
    /// </summary>
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetRecentActivity()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
            return Json(new { success = false, message = "User not authenticated" });

        var tests = await _testRepository.GetTestsByUserIdAsync(user.Id);
        var recentActivity = new List<object>();

        foreach (var test in tests)
        {
            var testData = await _testRepository.GetTestByIdAsync(test.Id);
            if (testData == null) continue;

            var recentAttempts = testData.Attempts
                .OrderByDescending(a => a.StartTime)
                .Take(5)
                .Select(a => new
                {
                    testId = test.Id,
                    testName = test.TestName,
                    studentName = $"{a.FirstName} {a.LastName}",
                    startTime = a.StartTime,
                    isCompleted = a.IsCompleted,
                    score = a.Score,
                    type = "attempt"
                });

            recentActivity.AddRange(recentAttempts);
        }

        var sortedActivity = recentActivity
            .OrderByDescending(a => ((dynamic)a).startTime)
            .Take(20);

        return Json(new { success = true, data = sortedActivity });
    }

    /// <summary>
    /// Exports test data in various formats
    /// </summary>
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> ExportTestData(string id, string format = "json")
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
            return Json(new { success = false, message = "User not authenticated" });

        var test = await _testRepository.GetTestByIdAsync(id);
        if (test is null)
            return Json(new { success = false, message = "Test not found" });

        if (test.User != user)
            return Json(new { success = false, message = "Unauthorized access" });

        var exportData = new
        {
            test = new
            {
                id = test.Id,
                name = test.TestName,
                description = test.Description,
                settings = new
                {
                    randomizeQuestions = test.RandomizeQuestions,
                    timeLimit = test.TimeLimit,
                    maxAttempts = test.MaxAttempts,
                    isLocked = test.IsLocked
                }
            },
            questions = test.Questions.OrderBy(q => q.Position).Select(q => new
            {
                id = q.Id,
                text = q.Text,
                points = q.Points,
                position = q.Position,
                type = q.GetType().Name.Replace("Question", "")
            }),
            statistics = await _testRepository.GetTestAnalyticsSummaryAsync(id)
        };

        if (format.ToLower() == "json")
        {
            return Json(new { success = true, data = exportData });
        }

        // Add other export formats here (CSV, PDF, etc.)
        return Json(new { success = false, message = "Unsupported export format" });
    }

    /// <summary>
    /// Gets test invite statistics
    /// </summary>
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetInviteStatistics(string id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
            return Json(new { success = false, message = "User not authenticated" });

        var test = await _testRepository.GetTestByIdAsync(id);
        if (test is null)
            return Json(new { success = false, message = "Test not found" });

        if (test.User != user)
            return Json(new { success = false, message = "Unauthorized access" });

        var invites = await _testInviteRepository.GetInvitesByTestIdAsync(id);
        var inviteStats = new
        {
            totalInvites = invites.Count(),
            usedInvites = invites.Count(i => i.IsUsed),
            unusedInvites = invites.Count(i => !i.IsUsed),
            invitesByEmail = invites.GroupBy(i => i.Email)
                .Select(g => new { email = g.Key, count = g.Count() })
                .OrderByDescending(x => x.count)
                .Take(10),
            recentInvites = invites.OrderByDescending(i => i.InviteSentDate)
                .Take(10)
                .Select(i => new
                {
                    email = i.Email,
                    sentDate = i.InviteSentDate,
                    isUsed = i.IsUsed
                })
        };

        return Json(new { success = true, data = inviteStats });
    }

    // DTO for quick updates
    public class QuickUpdateTestDto
    {
        public string TestId { get; set; }
        public string Property { get; set; }
        public object Value { get; set; }
    }
}