namespace TestPlatform2.Data;

public class TestInvite
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Email { get; set; } // Student email
    public bool IsUsed { get; set; } = false; // Has the student started the test?
    public DateTime InviteSentDate { get; set; } = DateTime.UtcNow;
    public string UniqueToken { get; set; } = Guid.NewGuid().ToString(); // For secure URL
    
     
    public string TestId { get; set; }
    public Test Test { get; set; }
}