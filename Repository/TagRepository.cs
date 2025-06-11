using Microsoft.EntityFrameworkCore;
using TestPlatform2.Data;

namespace TestPlatform2.Repository;

public class TagRepository : ITagRepository
{
    private readonly ApplicationDbContext _context;

    public TagRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<TestTag>> GetTagsByUserIdAsync(string userId)
    {
        return await _context.TestTags
            .Where(t => t.UserId == userId)
            .OrderBy(t => t.Name)
            .ToListAsync();
    }

    public async Task<TestTag?> GetTagByIdAsync(string tagId)
    {
        return await _context.TestTags
            .Include(t => t.Tests)
            .FirstOrDefaultAsync(t => t.Id == tagId);
    }

    public async Task<TestTag> CreateTagAsync(TestTag tag)
    {
        tag.CreatedAt = DateTime.UtcNow;
        _context.TestTags.Add(tag);
        await _context.SaveChangesAsync();
        return tag;
    }

    public async Task<TestTag> UpdateTagAsync(TestTag tag)
    {
        _context.TestTags.Update(tag);
        await _context.SaveChangesAsync();
        return tag;
    }

    public async Task DeleteTagAsync(string tagId)
    {
        var tag = await _context.TestTags
            .Include(t => t.Tests)
            .FirstOrDefaultAsync(t => t.Id == tagId);

        if (tag != null)
        {
            // Remove tag from all tests
            tag.Tests.Clear();
            _context.TestTags.Remove(tag);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> TagExistsAsync(string tagId, string userId)
    {
        return await _context.TestTags
            .AnyAsync(t => t.Id == tagId && t.UserId == userId);
    }

    public async Task<IEnumerable<TestTag>> GetTagsWithTestCountAsync(string userId)
    {
        return await _context.TestTags
            .Where(t => t.UserId == userId)
            .Select(t => new TestTag
            {
                Id = t.Id,
                Name = t.Name,
                Color = t.Color,
                CreatedAt = t.CreatedAt,
                UserId = t.UserId,
                Tests = t.Tests.Where(test => !test.IsArchived).ToList()
            })
            .OrderBy(t => t.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<TestTag>> GetTagsByNamesAsync(IEnumerable<string> tagNames, string userId)
    {
        return await _context.TestTags
            .Where(t => t.UserId == userId && tagNames.Contains(t.Name))
            .ToListAsync();
    }

    public async Task<IEnumerable<TestTag>> CreateTagsIfNotExistAsync(IEnumerable<string> tagNames, string userId)
    {
        var existingTags = await GetTagsByNamesAsync(tagNames, userId);
        var existingTagNames = existingTags.Select(t => t.Name).ToHashSet();
        
        var newTagNames = tagNames.Where(name => !existingTagNames.Contains(name)).ToList();
        var newTags = new List<TestTag>();

        foreach (var tagName in newTagNames)
        {
            var newTag = new TestTag
            {
                Name = tagName,
                UserId = userId,
                Color = GenerateRandomColor()
            };
            newTags.Add(newTag);
        }

        if (newTags.Any())
        {
            _context.TestTags.AddRange(newTags);
            await _context.SaveChangesAsync();
        }

        return existingTags.Concat(newTags);
    }

    private static string GenerateRandomColor()
    {
        var colors = new[]
        {
            "#EF4444", "#F97316", "#F59E0B", "#EAB308", "#84CC16",
            "#22C55E", "#10B981", "#14B8A6", "#06B6D4", "#0EA5E9",
            "#3B82F6", "#6366F1", "#8B5CF6", "#A855F7", "#D946EF",
            "#EC4899", "#F43F5E"
        };
        
        var random = new Random();
        return colors[random.Next(colors.Length)];
    }
}