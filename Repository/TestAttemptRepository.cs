using System.Collections;
using Microsoft.EntityFrameworkCore;
using TestPlatform2.Data;

namespace TestPlatform2.Repository;

// ITestAttemptRepository.cs
public interface ITestAttemptRepository
{
    Task<IEnumerable<TestAttempt?>> GetAttemptsByTestIdAsync(string testId);
    Task<IEnumerable<TestAttempt?>> GetFinishedAttemptsByTestIdAsync(string testId);
    Task<IEnumerable<TestAttempt?>> GetUnfinishedAttemptsByTestIdAsync(string testId);
    Task<TestAttempt?> GetAttemptByIdAsync(string attemptId);
    Task<TestAttempt?> GetAttemptUntrackedByIdAsync(string attemptId);
    Task Create(TestAttempt attempt);
    Task Update(TestAttempt attempt);
    Task Delete(TestAttempt attempt);
    Task MarkAsCompleted(string attemptId);
}

// TestAttemptRepository.cs
public class TestAttemptRepository : ITestAttemptRepository
{
    private readonly ApplicationDbContext _context;

    public TestAttemptRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<TestAttempt?>> GetAttemptsByTestIdAsync(string testId)
    {
        return await _context.TestAttempts
            .AsNoTracking()
            .Where(a => a.TestId == testId)
            .Include(a => a.Answers)
            .AsSplitQuery() // Split query to avoid cartesian product
            .OrderByDescending(a => a.StartTime) // Add ordering
            .ToListAsync();
    }
    public async Task<IEnumerable <TestAttempt?>> GetFinishedAttemptsByTestIdAsync(string testId)
    {
        return await _context.TestAttempts
            .Where(a => a.TestId == testId && a.IsCompleted == true)
            .Include(a => a.Answers)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<IEnumerable<TestAttempt?>> GetUnfinishedAttemptsByTestIdAsync(string testId)
    {
        return await _context.TestAttempts
            .Where(a => a.TestId == testId && a.IsCompleted == false)
            .Include(a => a.Answers)
            .AsNoTracking()
            .ToListAsync();
    }
    
    public async Task<TestAttempt?> GetAttemptByIdAsync(string attemptId)
    {
        return await _context.TestAttempts
            .AsSplitQuery() // Split to optimize multiple includes
            .Include(a => a.Answers)
            .ThenInclude(ans => ans.Question)
            .Include(a => a.Test)
            .ThenInclude(t => t.Questions)
            .FirstOrDefaultAsync(a => a.Id == attemptId);
    }

    public async Task<TestAttempt?> GetAttemptUntrackedByIdAsync(string attemptId)
    {
        return await _context.TestAttempts
            // .Include(a => a.Answers)
            // .Include(a => a.Test)
            // .ThenInclude(t => t.User)
            // .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == attemptId);
    }

    public async Task Create(TestAttempt attempt)
    {
        await _context.TestAttempts.AddAsync(attempt);
        await _context.SaveChangesAsync();
    }

    public async Task Update(TestAttempt attempt)
    {
        _context.TestAttempts.Update(attempt);
        await _context.SaveChangesAsync();
    }

    public async Task Delete(TestAttempt attempt)
    {
        _context.TestAttempts.Remove(attempt);
        await _context.SaveChangesAsync();
    }

    public async Task MarkAsCompleted(string attemptId)
    {
        var attempt = await _context.TestAttempts.FindAsync(attemptId);
        if (attempt != null)
        {
            attempt.IsCompleted = true;
            attempt.EndTime = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }
}