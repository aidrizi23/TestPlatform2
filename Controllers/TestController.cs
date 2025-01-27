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
    
    public TestController(ITestRepository testRepository, UserManager<User> userManager)
    {
        _testRepository = testRepository;
        _userManager = userManager;
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
            // If validation fails, return the view with the DTO to show validation errors
            return View(dto);
        }
        
        // Get the current authenticated user
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
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

        // Save the test to the database
        await _testRepository.Create(test);

        // Set a success message to display on the Index page
        TempData["SuccessMessage"] = "Test created successfully!";

        // Redirect to the Index action to show the list of tests
        return RedirectToAction("Index");
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
        
        // now just for security let's initialise a testViewModel
       
        
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
        
        // now i will create a testForEditDto
        var testForEditDto = new TestForEditDto
        {
            Id = test.Id,
            TestName = test.TestName,
            Description = test.Description,
            RandomizeQuestions = test.RandomizeQuestions,
            TimeLimit = test.TimeLimit,
            MaxAttempts = test.MaxAttempts
        };
        
        return View(testForEditDto);
    }
    
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Edit(TestForEditDto dto)
    {
        if (!ModelState.IsValid)
        {
            // If validation fails, return the view with the DTO to show validation errors
            return View(dto);
        }
        
        // get the user 
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return RedirectToAction("Login", "Account");
        }
        
        // get the test by id
        Test? test = await _testRepository.GetTestByIdAsync(dto.Id);

        if (test is null)
        {
            return NotFound();
        }
        
        if(test.User !=  user)
        {
            return Forbid();
        }
        
        // Map the DTO to the Test entity
        test.TestName = dto.TestName;
        test.Description = dto.Description;
        test.RandomizeQuestions = dto.RandomizeQuestions;
        test.TimeLimit = dto.TimeLimit;
        test.MaxAttempts = dto.MaxAttempts;
        
        // Save the test to the database
        await _testRepository.Update(test);

        // Set a success message to display on the Index page
        TempData["SuccessMessage"] = "Test updated successfully!";

        // Redirect to the Index action to show the list of tests
        return RedirectToAction("Index");
    }
    
    // method to direclty delete test
    
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Delete(string id)
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
        
        // delete the test
        await _testRepository.Delete(test);

        // Set a success message to display on the Index page
        TempData["SuccessMessage"] = "Test deleted successfully!";

        // Redirect to the Index action to show the list of tests
        return RedirectToAction("Index");
    }
    
    
    
}