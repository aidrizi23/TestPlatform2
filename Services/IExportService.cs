using TestPlatform2.Data;
using TestPlatform2.Models;
using TestPlatform2.Controllers;

namespace TestPlatform2.Services;

public interface IExportService
{
    Task<byte[]> ExportTestResultsToPdfAsync(Test test, IEnumerable<TestAttempt> attempts);
    Task<byte[]> ExportTestResultsToExcelAsync(Test test, IEnumerable<TestAttempt> attempts);
    Task<byte[]> ExportDetailedTestAnalyticsToPdfAsync(Test test, object analyticsData);
    Task<byte[]> ExportTestSummaryToPdfAsync(Test test);
    Task<byte[]> ExportAnalyticsToPdfAsync(TestAnalyticsViewModel analyticsData, TestController.AnalyticsExportRequest request);
    Task<byte[]> ExportAnalyticsToExcelAsync(TestAnalyticsViewModel analyticsData, TestController.AnalyticsExportRequest request);
    Task<byte[]> ExportAnalyticsToCsvAsync(TestAnalyticsViewModel analyticsData, TestController.AnalyticsExportRequest request);
}