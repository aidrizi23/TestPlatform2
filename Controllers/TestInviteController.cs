using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TestPlatform2.Data;
using TestPlatform2.Helpers;
using TestPlatform2.Repository;
using TestPlatform2.Services;
using Microsoft.AspNetCore.Identity;

namespace TestPlatform2.Controllers;

[Authorize]
public class TestInviteController : Controller
{
    private readonly ITestInviteRepository _inviteRepository;
    private readonly IEmailService _emailService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ITestRepository _testRepository;
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly UserManager<User> _userManager;

    public TestInviteController(
        ITestInviteRepository inviteRepository,
        IEmailService emailService,
        IHttpContextAccessor httpContextAccessor,
        ITestRepository testRepository,
        ISubscriptionRepository subscriptionRepository,
        UserManager<User> userManager)
    {
        _inviteRepository = inviteRepository;
        _emailService = emailService;
        _httpContextAccessor = httpContextAccessor;
        _testRepository = testRepository;
        _subscriptionRepository = subscriptionRepository;
        _userManager = userManager;
    }

    [HttpPost]
    public async Task<IActionResult> SendInvites(string testId, [FromBody] List<string> emails)
    {
        // Get the current user
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Unauthorized();
        
        // Get the test
        var test = await _testRepository.GetTestByIdAsync(testId);
        if (test is null) return NotFound();
        if (test.IsLocked) return BadRequest("Test is locked");
        if(test.Questions.Count == 0 || !test.Questions.Any()) return BadRequest("Test has no questions");
        
        // Check if user owns the test
        if (test.UserId != user.Id) return Unauthorized();
        
        // Check invite limits for each email
        var successfulInvites = new List<string>();
        var failedInvites = new List<string>();
        
        foreach (var email in emails)
        {
            // Check if user can send invite
            if (!await _subscriptionRepository.CanSendInviteAsync(user.Id))
            {
                failedInvites.Add(email);
                continue;
            }
            
            var invite = new TestInvite
            {
                TestId = testId,
                Email = email
            };
            await _inviteRepository.Create(invite);
            
            // Increment invite count
            await _subscriptionRepository.IncrementInviteCountAsync(user.Id);

            // Generate the test URL dynamically
            var testUrl = UrlHelper.GenerateTestUrl(HttpContext, testId, invite.UniqueToken);

            // Send the email
            try
            {
                await _emailService.SendEmailAsync(
                    email, 
                    "Test Invite", 
                    $"<h2>You've been invited to take a test!</h2>" +
                    $"<p>Click here to start the test: <a href='{testUrl}'>{testUrl}</a></p>" +
                    $"<p>You can also enter the following code in the app: <strong>{invite.UniqueToken}</strong></p>");
                
                successfulInvites.Add(email);
            }
            catch
            {
                failedInvites.Add(email);
            }
        }
        
        // Get remaining invites
        var remainingInvites = await _subscriptionRepository.GetRemainingWeeklyInvitesAsync(user.Id);
        
        // Return response with details
        return Ok(new 
        { 
            successfulInvites = successfulInvites,
            failedInvites = failedInvites,
            remainingInvites = remainingInvites,
            message = failedInvites.Any() && !user.IsPro
                ? "Some invites failed. You've reached your weekly limit. Upgrade to Pro for unlimited invites!"
                : "Invites sent successfully!"
        });
    }
}