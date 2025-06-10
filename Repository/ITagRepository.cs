using TestPlatform2.Data;

namespace TestPlatform2.Repository;

public interface ITagRepository
{
    Task<IEnumerable<TestTag>> GetTagsByUserIdAsync(string userId);
    Task<TestTag?> GetTagByIdAsync(string tagId);
    Task<TestTag> CreateTagAsync(TestTag tag);
    Task<TestTag> UpdateTagAsync(TestTag tag);
    Task DeleteTagAsync(string tagId);
    Task<bool> TagExistsAsync(string tagId, string userId);
    Task<IEnumerable<TestTag>> GetTagsWithTestCountAsync(string userId);
    Task<IEnumerable<TestTag>> GetTagsByNamesAsync(IEnumerable<string> tagNames, string userId);
    Task<IEnumerable<TestTag>> CreateTagsIfNotExistAsync(IEnumerable<string> tagNames, string userId);
}