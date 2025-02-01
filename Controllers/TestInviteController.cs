using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TestPlatform2.Data;
using TestPlatform2.Helpers;
using TestPlatform2.Repository;
using TestPlatform2.Services;

namespace TestPlatform2.Controllers;

[Authorize]
public class TestInviteController : Controller
{
    private readonly ITestInviteRepository _inviteRepository;
    private readonly IEmailService _emailService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public TestInviteController(
        ITestInviteRepository inviteRepository,
        IEmailService emailService,
        IHttpContextAccessor httpContextAccessor)
    {
        _inviteRepository = inviteRepository;
        _emailService = emailService;
        _httpContextAccessor = httpContextAccessor;
    }

    [HttpPost]
    public async Task<IActionResult> SendInvites(string testId, [FromBody] List<string> emails)
    {
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
            await _emailService.SendEmailAsync(email, "Test Invite", $"Click here to start the test: {testUrl}");
        }

        return Ok();
    }
}