using Microsoft.EntityFrameworkCore;
using TestPlatform2.Data;

namespace TestPlatform2.Repository;

// ITestInviteRepository.cs
public interface ITestInviteRepository
{
    Task<TestInvite?> GetInviteByTokenAsync(string token);
    Task<IEnumerable<TestInvite?>> GetInvitesByTestIdAsync(string testId);
    Task Create(TestInvite invite);
    Task Update(TestInvite invite);
    Task Delete(TestInvite invite);
    Task MarkAsUsed(string inviteId);
}

// TestInviteRepository.cs
public class TestInviteRepository : ITestInviteRepository
{
    private readonly ApplicationDbContext _context;

    public TestInviteRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<TestInvite?> GetInviteByTokenAsync(string token)
    {
        return await _context.TestInvites
            .Include(i => i.Test)
            .FirstOrDefaultAsync(i => i.UniqueToken == token);
    }

    public async Task<IEnumerable<TestInvite?>> GetInvitesByTestIdAsync(string testId)
    {
        return await _context.TestInvites
            .Where(i => i.TestId == testId)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task Create(TestInvite invite)
    {
        await _context.TestInvites.AddAsync(invite);
        await _context.SaveChangesAsync();
    }

    public async Task Update(TestInvite invite)
    {
        _context.TestInvites.Update(invite);
        await _context.SaveChangesAsync();
    }

    public async Task Delete(TestInvite invite)
    {
        _context.TestInvites.Remove(invite);
        await _context.SaveChangesAsync();
    }

    public async Task MarkAsUsed(string inviteId)
    {
        var invite = await _context.TestInvites.FindAsync(inviteId);
        if (invite != null)
        {
            invite.IsUsed = true;
            await _context.SaveChangesAsync();
        }
    }
}