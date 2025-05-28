namespace TestPlatform2.Models;

public class TestDeniedViewModel
{
    public string Title { get; set; }
    public string Message { get; set; }
    public TestDenialReason Reason { get; set; }
    public string TestName { get; set; }
    public string TestId { get; set; }
    public string IconClass { get; set; }
    public string AlertClass { get; set; }
    public bool ShowRetryButton { get; set; }
    public bool ShowContactSupport { get; set; }
    public string RetryUrl { get; set; }
    public Dictionary<string, object> AdditionalInfo { get; set; } = new();
}

public enum TestDenialReason
{
    TestTakenBefore,
    InvalidToken,
    TestLocked,
    TestNotFound,
    NoQuestionsAvailable,
    TestExpired,
    MaxAttemptsExceeded,
    InviteAlreadyUsed,
    UnauthorizedAccess
}