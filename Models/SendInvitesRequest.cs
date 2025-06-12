namespace TestPlatform2.Models;

public class SendInvitesRequest
{
    public string TestId { get; set; }
    public string EmailAddresses { get; set; }
    public string CustomMessage { get; set; }
}