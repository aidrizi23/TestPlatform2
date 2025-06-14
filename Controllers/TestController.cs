﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;
using TestPlatform2.Data;
using TestPlatform2.Data.Questions;
using TestPlatform2.Models;
using TestPlatform2.Repository;
using TestPlatform2.Services;
using System.Text.Json;

namespace TestPlatform2.Controllers;

public class TestController : Controller
{
    private readonly ITestRepository _testRepository;
    private readonly UserManager<User> _userManager;
    private readonly ITestAttemptRepository _testAttemptRepository;
    private readonly ITestAnalyticsRepository _testAnalyticsRepository;
    private readonly ITestInviteRepository _testInviteRepository;
    private readonly IQuestionRepository _questionRepository;
    private readonly IExportService _exportService;
    private readonly ICategoryRepository _categoryRepository;
    private readonly ITagRepository _tagRepository;
    
    public TestController(
        ITestRepository testRepository, 
        UserManager<User> userManager, 
        ITestAttemptRepository testAttemptRepository,
        ITestAnalyticsRepository testAnalyticsRepository,
        ITestInviteRepository testInviteRepository,
        IQuestionRepository questionRepository,
        IExportService exportService,
        ICategoryRepository categoryRepository,
        ITagRepository tagRepository)
    {
        _testRepository = testRepository;
        _userManager = userManager;
        _testAttemptRepository = testAttemptRepository;
        _testAnalyticsRepository = testAnalyticsRepository;
        _testInviteRepository = testInviteRepository;
        _questionRepository = questionRepository;
        _exportService = exportService;
        _categoryRepository = categoryRepository;
        _tagRepository = tagRepository;
    }
    
    // ========== TRADITIONAL MVC METHODS ==========
    
    // GET: /Test/Index
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Index(bool showArchived = false)
    {
        var user = await _userManager.GetUserAsync(User);
        var tests = await _testRepository.GetTestsByUserIdAsync(user.Id);
        
        // Filter archived tests based on the parameter
        if (!showArchived)
        {
            tests = tests.Where(t => !t.IsArchived).ToList();
        }
        
        ViewBag.ShowArchived = showArchived;
        return View(tests);
    }
    
    // GET: /Test/Create
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Create()
    {
        var dto = new TestForCreationDto();
        return View(dto);
    }

    // POST: /Test/Create (Traditional MVC)
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create(TestForCreationDto dto)
    {
        if (!ModelState.IsValid)
        {
            return View(dto);
        }
        
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return RedirectToAction("Login", "Account");
        }
        
        var test = new Test
        {
            TestName = dto.Title,
            Description = dto.Description,
            RandomizeQuestions = dto.RandomizeQuestions,
            TimeLimit = dto.TimeLimit,
            MaxAttempts = dto.MaxAttempts,
            UserId = user.Id,
            CategoryId = dto.CategoryId,
            // Scheduling properties
            IsScheduled = dto.IsScheduled,
            ScheduledStartDate = dto.ScheduledStartDate,
            ScheduledEndDate = dto.ScheduledEndDate,
            AutoPublish = dto.AutoPublish,
            AutoClose = dto.AutoClose
        };

        // Set the test status based on scheduling
        if (dto.IsScheduled)
        {
            var now = DateTime.UtcNow;
            if (dto.ScheduledStartDate.HasValue && dto.ScheduledStartDate > now)
            {
                test.Status = TestStatus.Scheduled;
                test.IsLocked = true; // Lock scheduled tests until they start
            }
            else if (dto.ScheduledEndDate.HasValue && dto.ScheduledEndDate < now)
            {
                test.Status = TestStatus.Closed;
                test.IsLocked = true; // Lock expired tests
            }
            else
            {
                test.Status = TestStatus.Active;
            }
        }
        else
        {
            test.Status = TestStatus.Active;
        }

        try
        {
            await _testRepository.Create(test);

            // Handle tags if provided
            if (dto.TagNames.Any())
            {
                var tags = await _tagRepository.CreateTagsIfNotExistAsync(dto.TagNames, user.Id);
                test.Tags = tags.ToList();
                await _testRepository.Update(test);
            }

            TempData["SuccessMessage"] = "Test created successfully!";
            return RedirectToAction("Index");
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", "An error occurred while creating the test.");
            return View(dto);
        }
    }

    // GET: /Test/Details
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Details(string id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return RedirectToAction("Login", "Account");
        }
        
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
    
    // GET: /Test/Edit
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Edit(string id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return RedirectToAction("Login", "Account");
        }
        
        Test? test = await _testRepository.GetTestByIdAsync(id);

        if (test is null)
        {
            return NotFound();
        }
        
        if(test.User !=  user)
        {
            return Forbid();
        }
        
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
        
        return View(testForEditDto);
    }
    
    // POST: /Test/Edit (Traditional MVC)
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Edit(TestForEditDto dto)
    {
        if (!ModelState.IsValid)
        {
            return View(dto);
        }
        
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return RedirectToAction("Login", "Account");
        }
        
        Test? test = await _testRepository.GetTestByIdAsync(dto.Id);

        if (test is null)
        {
            return NotFound();
        }
        
        if(test.User !=  user)
        {
            return Forbid();
        }

        try
        {
            test.TestName = dto.TestName;
            test.Description = dto.Description;
            test.RandomizeQuestions = dto.RandomizeQuestions;
            test.TimeLimit = dto.TimeLimit;
            test.MaxAttempts = dto.MaxAttempts;
            test.IsLocked = dto.IsLocked;
            
            await _testRepository.Update(test);
            TempData["SuccessMessage"] = "Test updated successfully!";
            return RedirectToAction("Index");
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", "An error occurred while updating the test.");
            return View(dto);
        }
    }
    
    // POST: /Test/Delete (Traditional MVC)
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Delete(string id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return RedirectToAction("Login", "Account");
        }
        
        Test? test = await _testRepository.GetTestByIdAsync(id);

        if (test is null)
        {
            return NotFound();
        }
        
        if(test.User !=  user)
        {
            return Forbid();
        }

        try
        {
            await _testRepository.Delete(test);
            TempData["SuccessMessage"] = "Test deleted successfully!";
            return RedirectToAction("Index");
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = "An error occurred while deleting the test.";
            return RedirectToAction("Index");
        }
    }
    
    // GET: /Test/AllAttempts
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> AllAttempts(string testId, string filter = "All")
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
            return RedirectToAction("Login", "Account");

        var test = await _testRepository.GetTestByIdAsync(testId);

        if (test is null)
            return NotFound();
        if (test.User != user)
            return Unauthorized();

        var allAttempts = await _testAttemptRepository.GetAttemptsByTestIdAsync(testId);
        var finishedAttempts = await _testAttemptRepository.GetFinishedAttemptsByTestIdAsync(testId);
        var unfinishedAttempts = await _testAttemptRepository.GetUnfinishedAttemptsByTestIdAsync(testId);

        var viewModel = new TestAttemptsViewModel
        {
            TestId = testId,
            AllAttempts = allAttempts,
            FinishedAttempts = finishedAttempts ?? Enumerable.Empty<TestAttempt>(),
            UnfinishedAttempts = unfinishedAttempts ?? Enumerable.Empty<TestAttempt>(),
            CurrentFilter = filter
        };

        return View(viewModel);
    }
    
    // GET: /Test/Analytics
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Analytics(string id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
            return RedirectToAction("Login", "Account");

        var test = await _testRepository.GetTestByIdAsync(id);
        if (test is null)
            return NotFound("Test not found");
                
        if (test.User != user)
            return Unauthorized("You do not have permission to view analytics for this test");

        var analyticsData = await _testAnalyticsRepository.GetTestAnalyticsAsync(id);
        if (analyticsData is null)
            return NotFound("Could not generate analytics for this test");

        return View(analyticsData);
    }
    
    // GET: /Test/QuestionAnalytics
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> QuestionAnalytics(string testId, string questionId)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
            return RedirectToAction("Login", "Account");

        var test = await _testRepository.GetTestByIdAsync(testId);
        if (test is null)
            return NotFound("Test not found");
        
        if (test.User != user)
            return Unauthorized("You do not have permission to view analytics for this test");
    
        var question = test.Questions.FirstOrDefault(q => q.Id == questionId);
        if (question is null)
            return NotFound("Question not found");
    
        var questionAnalytics = (await _testAnalyticsRepository.GetQuestionPerformanceDataAsync(testId))
            .FirstOrDefault(q => q.QuestionId == questionId);
    
        if (questionAnalytics is null)
            return NotFound("Could not generate analytics for this question");
    
        var viewModel = new QuestionAnalyticsViewModel
        {
            TestId = testId,
            TestName = test.TestName,
            Question = question,
            Analytics = questionAnalytics
        };
    
        return View(viewModel);
    }

    // POST: /Test/LockTest (Traditional MVC)
    [HttpPost]
    [Authorize]
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

        TempData["SuccessMessage"] = $"Test {(test.IsLocked ? "locked" : "unlocked")} successfully!";
        return RedirectToAction("Details", new { id = id });
    }

    // ========== AJAX METHODS ==========

    // POST: /Test/CreateAjax
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreateAjax([FromBody] TestForCreationDto dto)
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
        {
            return Json(new { success = false, message = "User not authenticated" });
        }
        
        var test = new Test
        {
            TestName = dto.Title,
            Description = dto.Description,
            RandomizeQuestions = dto.RandomizeQuestions,
            TimeLimit = dto.TimeLimit,
            MaxAttempts = dto.MaxAttempts,
            UserId = user.Id,
            CategoryId = dto.CategoryId,
            // Scheduling properties - ensure UTC conversion
            IsScheduled = dto.IsScheduled,
            ScheduledStartDate = dto.ScheduledStartDate?.ToUniversalTime(),
            ScheduledEndDate = dto.ScheduledEndDate?.ToUniversalTime(),
            AutoPublish = dto.AutoPublish,
            AutoClose = dto.AutoClose
        };

        // Set the test status based on scheduling
        if (dto.IsScheduled)
        {
            var now = DateTime.UtcNow;
            if (dto.ScheduledStartDate.HasValue && dto.ScheduledStartDate > now)
            {
                test.Status = TestStatus.Scheduled;
                test.IsLocked = true; // Lock scheduled tests until they start
            }
            else if (dto.ScheduledEndDate.HasValue && dto.ScheduledEndDate < now)
            {
                test.Status = TestStatus.Closed;
                test.IsLocked = true; // Lock expired tests
            }
            else
            {
                test.Status = TestStatus.Active;
            }
        }
        else
        {
            test.Status = TestStatus.Active;
        }

        try
        {
            await _testRepository.Create(test);

            // Handle tags if provided
            if (dto.TagNames.Any())
            {
                var tags = await _tagRepository.CreateTagsIfNotExistAsync(dto.TagNames, user.Id);
                test.Tags = tags.ToList();
                await _testRepository.Update(test);
            }

            return Json(new { 
                success = true, 
                message = "Test created successfully!",
                testId = test.Id,
                redirectUrl = Url.Action("Details", "Test", new { id = test.Id })
            });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "An error occurred while creating the test." });
        }
    }

    // GET: /Test/GetTestDataAjax
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetTestDataAjax(string id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
            return Json(new { success = false, message = "User not authenticated" });

        var test = await _testRepository.GetTestByIdAsync(id);
        if (test is null)
            return Json(new { success = false, message = "Test not found" });

        if (test.User != user)
            return Json(new { success = false, message = "Unauthorized access" });

        var testForEditDto = new TestForEditDto
        {
            Id = test.Id,
            TestName = test.TestName,
            Description = test.Description,
            RandomizeQuestions = test.RandomizeQuestions,
            TimeLimit = test.TimeLimit,
            MaxAttempts = test.MaxAttempts,
            IsLocked = test.IsLocked,
            // Scheduling properties - convert UTC to local for display
            IsScheduled = test.IsScheduled,
            ScheduledStartDate = test.ScheduledStartDate?.ToLocalTime(),
            ScheduledEndDate = test.ScheduledEndDate?.ToLocalTime(),
            AutoPublish = test.AutoPublish,
            AutoClose = test.AutoClose
        };

        return Json(new { success = true, data = testForEditDto });
    }
    
    // POST: /Test/EditAjax
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> EditAjax([FromBody] TestForEditDto dto)
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
        {
            return Json(new { success = false, message = "User not authenticated" });
        }
        
        Test? test = await _testRepository.GetTestByIdAsync(dto.Id);

        if (test is null)
        {
            return Json(new { success = false, message = "Test not found" });
        }
        
        if(test.User !=  user)
        {
            return Json(new { success = false, message = "Unauthorized access" });
        }

        try
        {
            test.TestName = dto.TestName;
            test.Description = dto.Description;
            test.RandomizeQuestions = dto.RandomizeQuestions;
            test.TimeLimit = dto.TimeLimit;
            test.MaxAttempts = dto.MaxAttempts;
            test.IsLocked = dto.IsLocked;
            
            // Update scheduling properties - ensure UTC conversion
            test.IsScheduled = dto.IsScheduled;
            test.ScheduledStartDate = dto.ScheduledStartDate?.ToUniversalTime();
            test.ScheduledEndDate = dto.ScheduledEndDate?.ToUniversalTime();
            test.AutoPublish = dto.AutoPublish;
            test.AutoClose = dto.AutoClose;
            
            // Set the test status based on scheduling
            if (dto.IsScheduled)
            {
                var now = DateTime.UtcNow;
                var startDateUtc = dto.ScheduledStartDate?.ToUniversalTime();
                var endDateUtc = dto.ScheduledEndDate?.ToUniversalTime();
                
                if (startDateUtc.HasValue && startDateUtc > now)
                {
                    test.Status = TestStatus.Scheduled;
                    test.IsLocked = true; // Lock scheduled tests until they start
                }
                else if (endDateUtc.HasValue && endDateUtc < now)
                {
                    test.Status = TestStatus.Closed;
                    test.IsLocked = true; // Lock expired tests
                }
                else
                {
                    test.Status = TestStatus.Active;
                }
            }
            else
            {
                test.Status = TestStatus.Active;
            }
            
            await _testRepository.Update(test);

            return Json(new { 
                success = true, 
                message = "Test updated successfully!",
                redirectUrl = Url.Action("Index")
            });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "An error occurred while updating the test." });
        }
    }
    
    // POST: /Test/DeleteAjax
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> DeleteAjax([FromBody] DeleteTestRequest request)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Json(new { success = false, message = "User not authenticated" });
        }
        
        Test? test = await _testRepository.GetTestByIdAsync(request.Id);

        if (test is null)
        {
            return Json(new { success = false, message = "Test not found" });
        }
        
        if(test.User !=  user)
        {
            return Json(new { success = false, message = "Unauthorized access" });
        }

        try
        {
            await _testRepository.Delete(test);

            return Json(new { 
                success = true, 
                message = "Test deleted successfully!"
            });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "An error occurred while deleting the test." });
        }
    }

    // POST: /Test/LockTestAjax
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> LockTestAjax([FromBody] LockTestRequest request)
    {
        var test = await _testRepository.GetTestByIdAsync(request.Id);
        if (test == null)
        {
            return Json(new { success = false, message = "Test not found" });
        }

        var user = await _userManager.GetUserAsync(User);
        if (user!.Id != test.UserId)
        {
            return Json(new { success = false, message = "Unauthorized access" });
        }

        try
        {
            test.IsLocked = !test.IsLocked;
            await _testRepository.Update(test);

            return Json(new { 
                success = true, 
                isLocked = test.IsLocked,
                message = $"Test {(test.IsLocked ? "locked" : "unlocked")} successfully!"
            });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "An error occurred while updating the test." });
        }
    }

    // GET: /Test/GetTestStatisticsAjax
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetTestStatisticsAjax(string id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
            return Json(new { success = false, message = "User not authenticated" });

        var test = await _testRepository.GetTestByIdAsync(id);
        if (test is null)
            return Json(new { success = false, message = "Test not found" });

        if (test.User != user)
            return Json(new { success = false, message = "Unauthorized access" });

        try
        {
            var stats = await _testRepository.GetTestAnalyticsSummaryAsync(id);
            if (stats == null)
                return Json(new { success = false, message = "Could not retrieve statistics" });

            return Json(new { success = true, data = stats });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Error retrieving statistics" });
        }
    }

    // GET: /Test/GetTestQuestionsAjax
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetTestQuestionsAjax(string id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
            return Json(new { success = false, message = "User not authenticated" });

        var test = await _testRepository.GetTestByIdAsync(id);
        if (test is null)
            return Json(new { success = false, message = "Test not found" });

        if (test.User != user)
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

    // POST: /Test/BulkDeleteTestsAjax
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> BulkDeleteTestsAjax([FromBody] BulkDeleteRequest request)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
            return Json(new { success = false, message = "User not authenticated" });

        if (request.TestIds == null || !request.TestIds.Any())
            return Json(new { success = false, message = "No tests selected" });

        try
        {
            var tests = await _testRepository.GetTestsByIdsAsync(request.TestIds, user.Id);
            
            if (!tests.Any())
                return Json(new { success = false, message = "No valid tests found to delete" });

            await _testRepository.BulkDeleteAsync(tests);

            return Json(new
            {
                success = true,
                message = $"Successfully deleted {tests.Count} test(s)",
                deletedCount = tests.Count
            });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "An error occurred while deleting tests" });
        }
    }

    // POST: /Test/BulkArchiveTestsAjax
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> BulkArchiveTestsAjax([FromBody] BulkArchiveRequest request)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
            return Json(new { success = false, message = "User not authenticated" });

        if (request.TestIds == null || !request.TestIds.Any())
            return Json(new { success = false, message = "No tests selected" });

        try
        {
            var tests = await _testRepository.GetTestsByIdsAsync(request.TestIds, user.Id);
            
            if (!tests.Any())
                return Json(new { success = false, message = "No valid tests found to update" });

            foreach (var test in tests)
            {
                test.IsArchived = request.Archive;
                test.ArchivedAt = request.Archive ? DateTime.UtcNow : null;
            }

            await _testRepository.BulkUpdateAsync(tests);

            var action = request.Archive ? "archived" : "unarchived";
            return Json(new
            {
                success = true,
                message = $"Successfully {action} {tests.Count} test(s)",
                updatedCount = tests.Count
            });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "An error occurred while updating tests" });
        }
    }

    // POST: /Test/BulkLockTestsAjax
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> BulkLockTestsAjax([FromBody] BulkLockRequest request)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
            return Json(new { success = false, message = "User not authenticated" });

        if (request.TestIds == null || !request.TestIds.Any())
            return Json(new { success = false, message = "No tests selected" });

        try
        {
            var tests = await _testRepository.GetTestsByIdsAsync(request.TestIds, user.Id);
            
            if (!tests.Any())
                return Json(new { success = false, message = "No valid tests found to update" });

            foreach (var test in tests)
            {
                test.IsLocked = request.Lock;
            }

            await _testRepository.BulkUpdateAsync(tests);

            var action = request.Lock ? "locked" : "unlocked";
            return Json(new
            {
                success = true,
                message = $"Successfully {action} {tests.Count} test(s)",
                updatedCount = tests.Count
            });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "An error occurred while updating test lock status" });
        }
    }

    // POST: /Test/CloneTestAjax
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CloneTestAjax([FromBody] CloneTestRequest request)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
            return Json(new { success = false, message = "User not authenticated" });

        var originalTest = await _testRepository.GetTestByIdAsync(request.Id);
        if (originalTest is null)
            return Json(new { success = false, message = "Test not found" });

        if (originalTest.User != user)
            return Json(new { success = false, message = "Unauthorized access" });

        try
        {
            // Create the cloned test
            var clonedTest = new Test
            {
                TestName = $"{originalTest.TestName} (Copy)",
                Description = originalTest.Description,
                RandomizeQuestions = originalTest.RandomizeQuestions,
                TimeLimit = originalTest.TimeLimit,
                MaxAttempts = originalTest.MaxAttempts,
                UserId = user.Id,
                IsLocked = false
            };

            await _testRepository.Create(clonedTest);

            // Clone all questions from the original test
            var originalQuestions = originalTest.Questions.OrderBy(q => q.Position).ToList();
            var clonedQuestions = new List<Question>();

            foreach (var originalQuestion in originalQuestions)
            {
                Question clonedQuestion = null;

                // Create new question based on type
                switch (originalQuestion)
                {
                    case TrueFalseQuestion tfq:
                        clonedQuestion = new TrueFalseQuestion
                        {
                            Text = tfq.Text,
                            Points = tfq.Points,
                            Position = tfq.Position,
                            TestId = clonedTest.Id,
                            CorrectAnswer = tfq.CorrectAnswer
                        };
                        break;

                    case MultipleChoiceQuestion mcq:
                        clonedQuestion = new MultipleChoiceQuestion
                        {
                            Text = mcq.Text,
                            Points = mcq.Points,
                            Position = mcq.Position,
                            TestId = clonedTest.Id,
                            Options = new List<string>(mcq.Options),
                            CorrectAnswers = new List<string>(mcq.CorrectAnswers),
                            AllowMultipleSelections = mcq.AllowMultipleSelections
                        };
                        break;

                    case ShortAnswerQuestion saq:
                        clonedQuestion = new ShortAnswerQuestion
                        {
                            Text = saq.Text,
                            Points = saq.Points,
                            Position = saq.Position,
                            TestId = clonedTest.Id,
                            ExpectedAnswer = saq.ExpectedAnswer,
                            CaseSensitive = saq.CaseSensitive
                        };
                        break;
                }

                if (clonedQuestion != null)
                {
                    clonedQuestions.Add(clonedQuestion);
                }
            }

            // Bulk create all cloned questions
            if (clonedQuestions.Any())
            {
                await _questionRepository.BulkCreate(clonedQuestions);
            }

            return Json(new
            {
                success = true,
                message = $"Test cloned successfully with {clonedQuestions.Count} questions",
                newTestId = clonedTest.Id,
                newTestName = clonedTest.TestName,
                questionsCloned = clonedQuestions.Count
            });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Error cloning test: " + ex.Message });
        }
    }

    // GET: /Test/GetFilteredTestsAjax
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetFilteredTestsAjax(bool showArchived = false, string categoryId = "", string tagId = "", string search = "")
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
            return Json(new { success = false, message = "User not authenticated" });

        try
        {
            var tests = await _testRepository.GetTestsByUserIdAsync(user.Id);
            
            // Filter archived tests
            if (!showArchived)
            {
                tests = tests.Where(t => !t.IsArchived).ToList();
            }
            else
            {
                tests = tests.Where(t => t.IsArchived).ToList();
            }

            // Filter by category if specified
            if (!string.IsNullOrEmpty(categoryId))
            {
                tests = tests.Where(t => t.CategoryId == categoryId).ToList();
            }

            // Filter by tag if specified
            if (!string.IsNullOrEmpty(tagId))
            {
                tests = tests.Where(t => t.Tags.Any(tag => tag.Id == tagId)).ToList();
            }

            // Filter by search term if specified
            if (!string.IsNullOrEmpty(search))
            {
                search = search.ToLower();
                tests = tests.Where(t => 
                    t.TestName.ToLower().Contains(search) ||
                    (t.Description != null && t.Description.ToLower().Contains(search))
                ).ToList();
            }

            var testData = tests.Select(t => new
            {
                id = t.Id,
                name = t.TestName,
                description = t.Description,
                isLocked = t.IsLocked,
                isArchived = t.IsArchived,
                createdDate = t.CreatedAt,
                questionCount = t.Questions.Count,
                attemptCount = t.Attempts.Count,
                categoryId = t.CategoryId,
                categoryName = t.Category?.Name,
                tags = t.Tags.Select(tag => new { id = tag.Id, name = tag.Name }).ToArray(),
                status = t.Status.ToString(),
                averageScore = t.Attempts.Any(a => a.IsCompleted) ? 
                    Math.Round(t.Attempts.Where(a => a.IsCompleted).Average(a => a.Score), 1) : 0
            }).ToArray();

            return Json(new { 
                success = true, 
                data = testData,
                showArchived = showArchived,
                totalCount = testData.Length
            });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Error retrieving filtered tests" });
        }
    }

    // GET: /Test/GetRecentActivityAjax
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetRecentActivityAjax()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
            return Json(new { success = false, message = "User not authenticated" });

        try
        {
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
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Error retrieving recent activity" });
        }
    }

    // GET: /Test/ExportTestDataAjax
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> ExportTestDataAjax(string id, string format = "json")
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
            return Json(new { success = false, message = "User not authenticated" });

        var test = await _testRepository.GetTestByIdAsync(id);
        if (test is null)
            return Json(new { success = false, message = "Test not found" });

        if (test.User != user)
            return Json(new { success = false, message = "Unauthorized access" });

        try
        {
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

            return Json(new { success = false, message = "Unsupported export format" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Error exporting test data" });
        }
    }

    // GET: /Test/GetInviteStatisticsAjax
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetInviteStatisticsAjax(string id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
            return Json(new { success = false, message = "User not authenticated" });

        var test = await _testRepository.GetTestByIdAsync(id);
        if (test is null)
            return Json(new { success = false, message = "Test not found" });

        if (test.User != user)
            return Json(new { success = false, message = "Unauthorized access" });

        try
        {
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
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Error retrieving invite statistics" });
        }
    }

    // POST: /Test/ToggleLockAjax
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> ToggleLockAjax([FromBody] LockTestRequest request)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
            return Json(new { success = false, message = "User not authenticated" });

        var test = await _testRepository.GetTestByIdAsync(request.Id);
        if (test is null)
            return Json(new { success = false, message = "Test not found" });

        if (test.User != user)
            return Json(new { success = false, message = "Unauthorized access" });

        try
        {
            // Toggle the lock status
            test.IsLocked = !test.IsLocked;
            await _testRepository.Update(test);
            
            var status = test.IsLocked ? "locked" : "unlocked";
            var message = $"Test has been {status} successfully!";
            
            return Json(new { 
                success = true, 
                message = message,
                isLocked = test.IsLocked,
                status = status
            });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "An error occurred while updating the test lock status." });
        }
    }

    // ========== EXPORT METHODS ==========

    // GET: /Test/ExportResultsPdf
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> ExportResultsPdf(string testId)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
            return RedirectToAction("Login", "Account");

        var test = await _testRepository.GetTestByIdAsync(testId);
        if (test is null)
            return NotFound("Test not found");

        if (test.User != user)
            return Unauthorized("You do not have permission to export this test");

        var attempts = await _testAttemptRepository.GetAttemptsByTestIdAsync(testId);
        
        try
        {
            var pdfBytes = await _exportService.ExportTestResultsToPdfAsync(test, attempts);
            var fileName = $"TestResults_{test.TestName}_{DateTime.Now:yyyyMMdd}.pdf";
            
            return File(pdfBytes, "application/pdf", fileName);
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = "Error generating PDF export";
            return RedirectToAction("AllAttempts", new { testId = testId });
        }
    }

    // GET: /Test/ExportResultsExcel
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> ExportResultsExcel(string testId)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
            return RedirectToAction("Login", "Account");

        var test = await _testRepository.GetTestByIdAsync(testId);
        if (test is null)
            return NotFound("Test not found");

        if (test.User != user)
            return Unauthorized("You do not have permission to export this test");

        var attempts = await _testAttemptRepository.GetAttemptsByTestIdAsync(testId);
        
        try
        {
            var excelBytes = await _exportService.ExportTestResultsToExcelAsync(test, attempts);
            var fileName = $"TestResults_{test.TestName}_{DateTime.Now:yyyyMMdd}.xlsx";
            
            return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = "Error generating Excel export";
            return RedirectToAction("AllAttempts", new { testId = testId });
        }
    }

    // GET: /Test/ExportTestSummaryPdf
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> ExportTestSummaryPdf(string testId)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
            return RedirectToAction("Login", "Account");

        var test = await _testRepository.GetTestByIdAsync(testId);
        if (test is null)
            return NotFound("Test not found");

        if (test.User != user)
            return Unauthorized("You do not have permission to export this test");

        try
        {
            var pdfBytes = await _exportService.ExportTestSummaryToPdfAsync(test);
            var fileName = $"TestSummary_{test.TestName}_{DateTime.Now:yyyyMMdd}.pdf";
            
            return File(pdfBytes, "application/pdf", fileName);
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = "Error generating test summary PDF";
            return RedirectToAction("Details", new { id = testId });
        }
    }

    // POST: /Test/ExportResultsAjax
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> ExportResultsAjax([FromBody] ExportRequest request)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
            return Json(new { success = false, message = "User not authenticated" });

        var test = await _testRepository.GetTestByIdAsync(request.TestId);
        if (test is null)
            return Json(new { success = false, message = "Test not found" });

        if (test.User != user)
            return Json(new { success = false, message = "Unauthorized access" });

        try
        {
            var attempts = await _testAttemptRepository.GetAttemptsByTestIdAsync(request.TestId);
            byte[] fileBytes;
            string fileName;
            string contentType;

            switch (request.Format.ToLower())
            {
                case "pdf":
                    fileBytes = await _exportService.ExportTestResultsToPdfAsync(test, attempts);
                    fileName = $"TestResults_{test.TestName}_{DateTime.Now:yyyyMMdd}.pdf";
                    contentType = "application/pdf";
                    break;
                case "excel":
                    fileBytes = await _exportService.ExportTestResultsToExcelAsync(test, attempts);
                    fileName = $"TestResults_{test.TestName}_{DateTime.Now:yyyyMMdd}.xlsx";
                    contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                    break;
                default:
                    return Json(new { success = false, message = "Unsupported export format" });
            }

            // Convert to base64 for AJAX response
            var base64File = Convert.ToBase64String(fileBytes);
            
            return Json(new { 
                success = true, 
                fileName = fileName,
                fileData = base64File,
                contentType = contentType
            });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Error generating export file" });
        }
    }


    // ========== REQUEST DTOs FOR AJAX METHODS ==========
    
    public class DeleteTestRequest
    {
        public string Id { get; set; }
    }

    public class LockTestRequest
    {
        public string Id { get; set; }
    }

    public class BulkDeleteRequest
    {
        public List<string> TestIds { get; set; }
    }

    public class CloneTestRequest
    {
        public string Id { get; set; }
    }

    public class BulkArchiveRequest
    {
        public List<string> TestIds { get; set; }
        public bool Archive { get; set; }
    }

    public class BulkLockRequest
    {
        public List<string> TestIds { get; set; }
        public bool Lock { get; set; }
    }

    public class ExportRequest
    {
        public string TestId { get; set; }
        public string Format { get; set; } // "pdf" or "excel"
    }

}