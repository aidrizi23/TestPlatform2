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
            .Include(x => x.Questions)
            .Include(x => x.Attempts)
            .Include(x => x.InvitedStudents)
            .Include(x => x.User) // Include user details
            .AsNoTracking()
            .ToListAsync();
    }
    
    public async Task<Test?> GetTestByIdAsync(string testId)
    {
        return await _context.Tests
            .AsSplitQuery() // Critical for multiple collection includes
            .Include(x => x.User)
            .Include(x => x.Questions.OrderBy(q => q.Position)) // Order questions
            .Include(x => x.Attempts)
            .Include(x => x.InvitedStudents)
            .FirstOrDefaultAsync(t => t.Id == testId);
    }
    
    public async Task<Dictionary<string, object>> GetTestAnalyticsSummaryAsync(string testId)
    {
        var result = await _context.Tests
            .Where(t => t.Id == testId)
            .Select(t => new
            {
                TotalQuestions = t.Questions.Count(),
                TotalPoints = t.Questions.Sum(q => q.Points),
                TotalAttempts = t.Attempts.Count(),
                CompletedAttempts = t.Attempts.Count(a => a.IsCompleted),
                AverageScore = t.Attempts.Where(a => a.IsCompleted).Average(a => (double?)a.Score) ?? 0,
                LastAttemptDate = t.Attempts.Max(a => (DateTime?)a.StartTime)
            })
            .FirstOrDefaultAsync();

        if (result == null)
            return null;

        return new Dictionary<string, object>
        {
            ["TotalQuestions"] = result.TotalQuestions,
            ["TotalPoints"] = result.TotalPoints,
            ["TotalAttempts"] = result.TotalAttempts,
            ["CompletedAttempts"] = result.CompletedAttempts,
            ["AverageScore"] = result.AverageScore,
            ["LastAttemptDate"] = result.LastAttemptDate
        };
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

    public async Task<List<Test>> GetTestsByIdsAsync(List<string> testIds, string userId)
    {
        return await _context.Tests
            .Where(t => testIds.Contains(t.Id) && t.UserId == userId)
            .Include(t => t.Questions)
            .Include(t => t.Attempts)
            .ToListAsync();
    }

    public async Task BulkDeleteAsync(List<Test> tests)
    {
        _context.Tests.RemoveRange(tests);
        await _context.SaveChangesAsync();
    }

    public async Task BulkUpdateAsync(List<Test> tests)
    {
        _context.Tests.UpdateRange(tests);
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
    Task<Dictionary<string, object>> GetTestAnalyticsSummaryAsync(string testId);
    Task<List<Test>> GetTestsByIdsAsync(List<string> testIds, string userId);
    Task BulkDeleteAsync(List<Test> tests);
    Task BulkUpdateAsync(List<Test> tests);
}

