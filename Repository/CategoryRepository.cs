using Microsoft.EntityFrameworkCore;
using TestPlatform2.Data;

namespace TestPlatform2.Repository;

public class CategoryRepository : ICategoryRepository
{
    private readonly ApplicationDbContext _context;

    public CategoryRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<TestCategory>> GetCategoriesByUserIdAsync(string userId)
    {
        return await _context.TestCategories
            .Where(c => c.UserId == userId)
            .OrderBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<TestCategory?> GetCategoryByIdAsync(string categoryId)
    {
        return await _context.TestCategories
            .Include(c => c.Tests)
            .FirstOrDefaultAsync(c => c.Id == categoryId);
    }

    public async Task<TestCategory> CreateCategoryAsync(TestCategory category)
    {
        category.CreatedAt = DateTime.UtcNow;
        _context.TestCategories.Add(category);
        await _context.SaveChangesAsync();
        return category;
    }

    public async Task<TestCategory> UpdateCategoryAsync(TestCategory category)
    {
        category.UpdatedAt = DateTime.UtcNow;
        _context.TestCategories.Update(category);
        await _context.SaveChangesAsync();
        return category;
    }

    public async Task DeleteCategoryAsync(string categoryId)
    {
        var category = await _context.TestCategories.FindAsync(categoryId);
        if (category != null)
        {
            // Set all tests in this category to have no category
            var testsInCategory = await _context.Tests
                .Where(t => t.CategoryId == categoryId)
                .ToListAsync();

            foreach (var test in testsInCategory)
            {
                test.CategoryId = null;
            }

            _context.TestCategories.Remove(category);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> CategoryExistsAsync(string categoryId, string userId)
    {
        return await _context.TestCategories
            .AnyAsync(c => c.Id == categoryId && c.UserId == userId);
    }

    public async Task<IEnumerable<TestCategory>> GetCategoriesWithTestCountAsync(string userId)
    {
        return await _context.TestCategories
            .Where(c => c.UserId == userId)
            .Select(c => new TestCategory
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                Color = c.Color,
                Icon = c.Icon,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt,
                UserId = c.UserId,
                Tests = c.Tests.Where(t => !t.IsArchived).ToList()
            })
            .OrderBy(c => c.Name)
            .ToListAsync();
    }
}