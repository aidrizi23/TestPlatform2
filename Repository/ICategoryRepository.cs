using TestPlatform2.Data;

namespace TestPlatform2.Repository;

public interface ICategoryRepository
{
    Task<IEnumerable<TestCategory>> GetCategoriesByUserIdAsync(string userId);
    Task<TestCategory?> GetCategoryByIdAsync(string categoryId);
    Task<TestCategory> CreateCategoryAsync(TestCategory category);
    Task<TestCategory> UpdateCategoryAsync(TestCategory category);
    Task DeleteCategoryAsync(string categoryId);
    Task<bool> CategoryExistsAsync(string categoryId, string userId);
    Task<IEnumerable<TestCategory>> GetCategoriesWithTestCountAsync(string userId);
}