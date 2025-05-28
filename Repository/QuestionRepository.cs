using Microsoft.EntityFrameworkCore;
using TestPlatform2.Data;
using TestPlatform2.Data.Questions;

namespace TestPlatform2.Repository;

public class QuestionRepository : IQuestionRepository
{
    private readonly ApplicationDbContext _context;
    
    public QuestionRepository(ApplicationDbContext context)
    {
        _context = context;
    }
    
    // get all the questionsByTestId
    public async Task<IEnumerable<Question>> GetQuestionsByTestIdAsync(string testId)
    {
        return await _context.Questions
            .Where(q => q.TestId == testId)
            .ToListAsync();
    }
    
    // get a question by its id
    public async Task<Question?> GetQuestionByIdAsync(string questionId)
    {
        return await _context.Questions
            .Where(q => q.Id == questionId)
            .Include(x => x.Test)
            .ThenInclude(x => x.Attempts)
            .ThenInclude(x => x.Answers)
            .FirstOrDefaultAsync();
    }
    
    // create a question
    public async Task Create(Question question)
    {
        await _context.Questions.AddAsync(question);
        await _context.SaveChangesAsync();
    }
    
    // update a question
    public async Task Update(Question question)
    {
        _context.Questions.Update(question);
        await _context.SaveChangesAsync();
    }
    
    // delete a question
    public async Task Delete(Question question)
    {
        _context.Questions.Remove(question);
        await _context.SaveChangesAsync();
    }
    
    
}

public interface IQuestionRepository
{
    Task<IEnumerable<Question>> GetQuestionsByTestIdAsync(string testId);
    Task<Question?> GetQuestionByIdAsync(string questionId);
    Task Create(Question question);
    Task Update(Question question);
    Task Delete(Question question);
}