using Microsoft.AspNetCore.Identity;

namespace TestPlatform2.Data;

public class User : IdentityUser
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string FullName => $"{FirstName} {LastName}";

    public virtual ICollection<Test> Tests { get; set; } = new List<Test>();
    
    
    public DateTime RegistrationDate { get; set; } = DateTime.UtcNow;
}