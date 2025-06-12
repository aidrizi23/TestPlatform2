    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using TestPlatform2.Data;
    using TestPlatform2.Helpers;
    using TestPlatform2.Repository;
    using TestPlatform2.Services;
    using Microsoft.AspNetCore.Identity;
    using TestPlatform2.Models;

    namespace TestPlatform2.Controllers;

    [Authorize]
    public class TestInviteController : Controller
    {
    private readonly ITestInviteRepository _inviteRepository;
    private readonly IEmailService _emailService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ITestRepository _testRepository;
    private readonly UserManager<User> _userManager;

    public TestInviteController(
    ITestInviteRepository inviteRepository,
    IEmailService emailService,
    IHttpContextAccessor httpContextAccessor,
    ITestRepository testRepository,
    UserManager<User> userManager)
    {
    _inviteRepository = inviteRepository;
    _emailService = emailService;
    _httpContextAccessor = httpContextAccessor;
    _testRepository = testRepository;
    _userManager = userManager;
    }

    [HttpPost]
    public async Task<IActionResult> SendInvites([FromBody] SendInvitesRequest request)
    {
    // Parse email addresses from the request
    var emails = request.EmailAddresses
    .Split(',', StringSplitOptions.RemoveEmptyEntries)
    .Select(e => e.Trim())
    .Where(e => !string.IsNullOrEmpty(e))
    .ToList();

    if (!emails.Any())
    {
    return BadRequest("No valid email addresses provided");
    }

    var testId = request.TestId;

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

    var invite = new TestInvite
    {
    TestId = testId,
    Email = email
    };
    await _inviteRepository.Create(invite);


    // Generate the test URL dynamically
    var testUrl = UrlHelper.GenerateTestUrl(HttpContext, testId, invite.UniqueToken);

    // Send the email
    try
    {
    var emailBody = $"<h2>You've been invited to take a test!</h2>" +
              $"<p>Click here to start the test: <a href='{testUrl}'>{testUrl}</a></p>" +
              $"<p>You can also enter the following code in the app: <strong>{invite.UniqueToken}</strong></p>";

    // Add custom message if provided
    if (!string.IsNullOrEmpty(request.CustomMessage))
    {
    emailBody = $"<p><em>{request.CustomMessage}</em></p>" + emailBody;
    }

    await _emailService.SendEmailAsync(email, "Test Invite", emailBody);
    successfulInvites.Add(email);
    }
    catch
    {
    failedInvites.Add(email);
    }
    }

    // Get remaining invites

    // Return response with details (enhanced to match frontend expectations)
    return Ok(new 
    { 
    success = successfulInvites.Any(),
    sentCount = successfulInvites.Count,
    successfulInvites = successfulInvites,
    failedInvites = failedInvites,
    message = "Invites sent successfully!"
    });
    } 
    }