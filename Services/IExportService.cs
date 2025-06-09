using TestPlatform2.Data;

namespace TestPlatform2.Services;

public interface IExportService
{
    Task<byte[]> ExportTestResultsToPdfAsync(Test test, IEnumerable<TestAttempt> attempts);
    Task<byte[]> ExportTestResultsToExcelAsync(Test test, IEnumerable<TestAttempt> attempts);
    Task<byte[]> ExportDetailedTestAnalyticsToPdfAsync(Test test, object analyticsData);
    Task<byte[]> ExportTestSummaryToPdfAsync(Test test);
}