using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TestPlatform2.Data;

namespace TestPlatform2.Repository;

public class TestRepository : ITestRepository
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<User> _userManager;
    
    public TestRepository(ApplicationDbContext context, UserManager<User> userManager)
    {
        _context = context;
        _userManager = userManager;
    }
    
    public async Task<IEnumerable<Test>> GetTestsByUserIdAsync(string userId)
    {
        return await _context.Tests
            .Where(t => t.UserId == userId)
            .AsNoTracking()
            .ToListAsync();
    }
    
    public async Task<Test?> GetTestByIdAsync(string testId)
    {
        return await _context.Tests
            .Where(t => t.Id == testId)
            .Include(x => x.User)
            .Include(x => x.Questions)
            .FirstOrDefaultAsync();
    }

    public async Task Create(Test test)
    {
        await _context.Tests.AddAsync(test);
        await _context.SaveChangesAsync();
    }
    
    public async Task Update(Test test)
    {
        _context.Tests.Update(test);
        await _context.SaveChangesAsync();
    }
    
    public async Task Delete(Test test)
    {
        _context.Tests.Remove(test);
        await _context.SaveChangesAsync();
    }
    
}

public interface ITestRepository
{
    Task<IEnumerable<Test>> GetTestsByUserIdAsync(string userId);
    Task<Test?> GetTestByIdAsync(string testId);
    Task Create(Test test);
    Task Update(Test test);
    Task Delete(Test test);
}

