using Microsoft.EntityFrameworkCore;
using TestPlatform2.Data;

namespace TestPlatform2.Repository;

// IAnswerRepository.cs
public interface IAnswerRepository
{
    Task<IEnumerable<Answer>> GetAnswersByAttemptIdAsync(string attemptId);
    Task<Answer?> GetByIdAsync(string answerId);
    Task Create(Answer answer);
    Task Update(Answer answer);
    Task Delete(Answer answer);
    Task BulkDelete(IEnumerable<Answer> answers);
}

// AnswerRepository.cs
public class AnswerRepository : IAnswerRepository
{
    private readonly ApplicationDbContext _context;

    public AnswerRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Answer>> GetAnswersByAttemptIdAsync(string attemptId)
    {
        
        return await _context.Answers
            .Where(a => a.AttemptId == attemptId)
            .Include(a => a.Question)
            .Include(x => x.Attempt)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<Answer?> GetByIdAsync(string answerId)
    {
        return await _context.Answers
            .Include(a => a.Question)
            .FirstOrDefaultAsync(a => a.Id == answerId);
    }

    public async Task Create(Answer answer)
    {
        await _context.Answers.AddAsync(answer);
        await _context.SaveChangesAsync();
    }

    public async Task Update(Answer answer)
    {
        _context.Answers.Update(answer);
        await _context.SaveChangesAsync();
    }

    public async Task Delete(Answer answer)
    {
        _context.Answers.Remove(answer);
        await _context.SaveChangesAsync();
    }
    
    public async Task BulkDelete(IEnumerable<Answer> answers)
    {
        _context.Answers.RemoveRange(answers);
        await _context.SaveChangesAsync();
    }
}