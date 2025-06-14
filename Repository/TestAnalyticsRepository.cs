using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TestPlatform2.Data;
using TestPlatform2.Data.Questions;
using TestPlatform2.Models;

namespace TestPlatform2.Repository
{
    public interface ITestAnalyticsRepository
    {
        Task<TestAnalyticsViewModel> GetTestAnalyticsAsync(string testId);
        Task<List<QuestionAnalyticsData>> GetQuestionPerformanceDataAsync(string testId);
        Task<double> CalculateAverageScoreAsync(string testId);
        Task<double> CalculateMedianScoreAsync(string testId);
        Task<List<StudentPerformanceData>> GetTopPerformersAsync(string testId, int count = 5);
        Task<List<StudentPerformanceData>> GetStrugglingStudentsAsync(string testId, int count = 5);
        Task<List<RecentAttemptData>> GetRecentAttemptsAsync(string testId, int count = 10);
    }

    public class TestAnalyticsRepository : ITestAnalyticsRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly IAnswerRepository _answerRepository;

        public TestAnalyticsRepository(ApplicationDbContext context, IAnswerRepository answerRepository)
        {
            _context = context;
            _answerRepository = answerRepository;
        }

        public async Task<TestAnalyticsViewModel> GetTestAnalyticsAsync(string testId)
        {
            // Get the test
            var test = await _context.Tests
                .Include(t => t.Questions)
                .Include(t => t.Attempts)
                .FirstOrDefaultAsync(t => t.Id == testId);

            if (test == null)
                return null;

            // Get all completed attempts
            var allAttempts = test.Attempts.ToList();
            var completedAttempts = allAttempts.Where(a => a.IsCompleted).ToList();
            var inProgressAttempts = allAttempts.Where(a => !a.IsCompleted).ToList();

            // Create the analytics view model
            var analyticsViewModel = new TestAnalyticsViewModel
            {
                TestId = test.Id,
                TestName = test.TestName,
                Description = test.Description,
                TotalAttempts = allAttempts.Count,
                CompletedAttempts = completedAttempts.Count,
                InProgressAttempts = inProgressAttempts.Count
            };

            // Calculate overall performance metrics
            if (completedAttempts.Any())
            {
                // Average score
                analyticsViewModel.AverageScore = completedAttempts.Average(a => a.Score);

                // Median score
                var sortedScores = completedAttempts.Select(a => a.Score).OrderBy(s => s).ToList();
                int mid = sortedScores.Count / 2;
                analyticsViewModel.MedianScore = (sortedScores.Count % 2 != 0)
                    ? sortedScores[mid]
                    : (sortedScores[mid - 1] + sortedScores[mid]) / 2;

                // Highest and lowest scores
                analyticsViewModel.HighestScore = completedAttempts.Max(a => a.Score);
                analyticsViewModel.LowestScore = completedAttempts.Min(a => a.Score);

                // Standard deviation
                double mean = analyticsViewModel.AverageScore;
                double sumOfSquaresOfDifferences = completedAttempts.Sum(a => Math.Pow(a.Score - mean, 2));
                analyticsViewModel.StandardDeviation = Math.Sqrt(sumOfSquaresOfDifferences / completedAttempts.Count);

                // Calculate passing rate (assuming 60% is passing)
                double totalPoints = test.Questions.Sum(q => q.Points);
                int passedCount = completedAttempts.Count(a => (a.Score / totalPoints) >= 0.6);
                analyticsViewModel.PassingRate = (double)passedCount / completedAttempts.Count * 100;

                // Time metrics
                var attemptsWithCompletionTime = completedAttempts
                    .Where(a => a.EndTime.HasValue)
                    .Select(a => new { CompletionTime = a.EndTime.Value - a.StartTime })
                    .ToList();

                if (attemptsWithCompletionTime.Any())
                {
                    analyticsViewModel.AverageCompletionTime = TimeSpan.FromTicks(
                        (long)attemptsWithCompletionTime.Average(a => a.CompletionTime.Ticks));
                    analyticsViewModel.FastestCompletionTime = TimeSpan.FromTicks(
                        attemptsWithCompletionTime.Min(a => a.CompletionTime.Ticks));
                    analyticsViewModel.SlowestCompletionTime = TimeSpan.FromTicks(
                        attemptsWithCompletionTime.Max(a => a.CompletionTime.Ticks));
                }

                // Score distribution (0-10, 11-20, ..., 91-100)
                analyticsViewModel.ScoreDistribution = new List<int>();
                analyticsViewModel.ScoreRanges = new List<string>();

                for (int i = 0; i < 10; i++)
                {
                    int lowerBound = i * 10;
                    int upperBound = (i + 1) * 10;
                    double lowerPercentage = lowerBound / 100.0;
                    double upperPercentage = upperBound / 100.0;

                    int count = completedAttempts.Count(
                        a => (a.Score / totalPoints) >= lowerPercentage && 
                             (a.Score / totalPoints) < upperPercentage);

                    analyticsViewModel.ScoreDistribution.Add(count);
                    analyticsViewModel.ScoreRanges.Add($"{lowerBound}-{upperBound}%");
                }

                // Add the 100% range separately
                int perfectScores = completedAttempts.Count(
                    a => Math.Abs(a.Score - totalPoints) < 0.01); // Account for floating point imprecision
                analyticsViewModel.ScoreDistribution.Add(perfectScores);
                analyticsViewModel.ScoreRanges.Add("100%");
            }

            // Get question performance data
            analyticsViewModel.QuestionPerformance = await GetQuestionPerformanceDataAsync(testId);

            // Get top performers and struggling students
            analyticsViewModel.TopPerformers = await GetTopPerformersAsync(testId);
            analyticsViewModel.StrugglingSudents = await GetStrugglingStudentsAsync(testId);

            // Get recent attempts
            analyticsViewModel.RecentAttempts = await GetRecentAttemptsAsync(testId);

            // Get timeline data (daily completion counts for the last 7 days)
            analyticsViewModel.TimelineData = await GetTimelineDataAsync(testId);

            return analyticsViewModel;
        }

        public async Task<List<QuestionAnalyticsData>> GetQuestionPerformanceDataAsync(string testId)
        {
            // Get the test with questions
            var test = await _context.Tests
                .Include(t => t.Questions)
                .FirstOrDefaultAsync(t => t.Id == testId);

            if (test == null)
                return new List<QuestionAnalyticsData>();

            var questionAnalytics = new List<QuestionAnalyticsData>();

            // Get all completed attempts for this test
            var completedAttemptIds = await _context.TestAttempts
                .Where(a => a.TestId == testId && a.IsCompleted)
                .Select(a => a.Id)
                .ToListAsync();

            // Process each question
            foreach (var question in test.Questions)
            {
                // Get all answers for this question across all completed attempts
                var answers = await _context.Answers
                    .Where(a => a.QuestionId == question.Id && completedAttemptIds.Contains(a.AttemptId))
                    .ToListAsync();

                int correctAnswers = 0;
                int incorrectAnswers = 0;
                var answerDistribution = new Dictionary<string, int>();

                foreach (var answer in answers)
                {
                    // Count correct and incorrect answers
                    if (Math.Abs(answer.PointsAwarded - question.Points) < 0.01) // Correct answer
                        correctAnswers++;
                    else
                        incorrectAnswers++;

                    // Track answer distribution for multiple choice questions
                    if (question is MultipleChoiceQuestion)
                    {
                        string responseKey = answer.Response ?? "No Response";
                        
                        if (answerDistribution.ContainsKey(responseKey))
                            answerDistribution[responseKey]++;
                        else
                            answerDistribution[responseKey] = 1;
                    }
                }

                // Create question analytics data
                var questionType = question.GetType().Name.Replace("Question", "");
                double successRate = answers.Any() ? (double)correctAnswers / answers.Count * 100 : 0;
                double averagePoints = answers.Any() ? answers.Average(a => a.PointsAwarded) : 0;

                var questionData = new QuestionAnalyticsData
                {
                    QuestionId = question.Id,
                    QuestionText = question.Text,
                    QuestionType = questionType,
                    Position = question.Position,
                    Points = question.Points,
                    AveragePoints = averagePoints,
                    SuccessRate = successRate,
                    CorrectAnswers = correctAnswers,
                    IncorrectAnswers = incorrectAnswers,
                    AnswerDistribution = answerDistribution
                };

                questionAnalytics.Add(questionData);
            }

            // Sort by position
            return questionAnalytics.OrderBy(q => q.Position).ToList();
        }

        public async Task<double> CalculateAverageScoreAsync(string testId)
        {
            var completedAttempts = await _context.TestAttempts
                .Where(a => a.TestId == testId && a.IsCompleted)
                .ToListAsync();

            if (!completedAttempts.Any())
                return 0;

            return completedAttempts.Average(a => a.Score);
        }

        public async Task<double> CalculateMedianScoreAsync(string testId)
        {
            var scores = await _context.TestAttempts
                .Where(a => a.TestId == testId && a.IsCompleted)
                .Select(a => a.Score)
                .OrderBy(s => s)
                .ToListAsync();

            if (!scores.Any())
                return 0;

            int mid = scores.Count / 2;
            return (scores.Count % 2 != 0) 
                ? scores[mid] 
                : (scores[mid - 1] + scores[mid]) / 2;
        }

        public async Task<List<StudentPerformanceData>> GetTopPerformersAsync(string testId, int count = 5)
        {
            var test = await _context.Tests
                .Include(t => t.Questions)
                .FirstOrDefaultAsync(t => t.Id == testId);

            if (test == null)
                return new List<StudentPerformanceData>();

            double totalPoints = test.Questions.Sum(q => q.Points);

            var topPerformers = await _context.TestAttempts
                .Where(a => a.TestId == testId && a.IsCompleted && a.EndTime.HasValue)
                .OrderByDescending(a => a.Score)
                .Take(count)
                .Select(a => new StudentPerformanceData
                {
                    AttemptId = a.Id,
                    StudentName = $"{a.FirstName} {a.LastName}",
                    StudentEmail = a.StudentEmail,
                    Score = a.Score,
                    ScorePercentage = totalPoints > 0 ? a.Score / totalPoints * 100 : 0,
                    CompletionTime = a.EndTime.Value - a.StartTime,
                    CompletionDate = a.EndTime.Value
                })
                .ToListAsync();

            return topPerformers;
        }

        public async Task<List<StudentPerformanceData>> GetStrugglingStudentsAsync(string testId, int count = 5)
        {
            var test = await _context.Tests
                .Include(t => t.Questions)
                .FirstOrDefaultAsync(t => t.Id == testId);

            if (test == null)
                return new List<StudentPerformanceData>();

            double totalPoints = test.Questions.Sum(q => q.Points);

            var strugglingStudents = await _context.TestAttempts
                .Where(a => a.TestId == testId && a.IsCompleted && a.EndTime.HasValue)
                .OrderBy(a => a.Score)
                .Take(count)
                .Select(a => new StudentPerformanceData
                {
                    AttemptId = a.Id,
                    StudentName = $"{a.FirstName} {a.LastName}",
                    StudentEmail = a.StudentEmail,
                    Score = a.Score,
                    ScorePercentage = totalPoints > 0 ? a.Score / totalPoints * 100 : 0,
                    CompletionTime = a.EndTime.Value - a.StartTime,
                    CompletionDate = a.EndTime.Value
                })
                .ToListAsync();

            return strugglingStudents;
        }

        public async Task<List<RecentAttemptData>> GetRecentAttemptsAsync(string testId, int count = 10)
        {
            var test = await _context.Tests
                .Include(t => t.Questions)
                .FirstOrDefaultAsync(t => t.Id == testId);

            if (test == null)
                return new List<RecentAttemptData>();

            double totalPoints = test.Questions.Sum(q => q.Points);

            var recentAttempts = await _context.TestAttempts
                .Where(a => a.TestId == testId)
                .OrderByDescending(a => a.EndTime ?? a.StartTime)
                .Take(count)
                .Select(a => new RecentAttemptData
                {
                    AttemptId = a.Id,
                    StudentName = $"{a.FirstName} {a.LastName}",
                    StudentEmail = a.StudentEmail,
                    Score = a.Score,
                    ScorePercentage = totalPoints > 0 ? a.Score / totalPoints * 100 : 0,
                    CompletionDate = a.EndTime ?? a.StartTime,
                    IsCompleted = a.IsCompleted
                })
                .ToListAsync();

            return recentAttempts;
        }

        private async Task<List<DailyCompletionData>> GetTimelineDataAsync(string testId)
        {
            var timelineData = new List<DailyCompletionData>();
            
            // Get the last 7 days
            for (int i = 6; i >= 0; i--)
            {
                var date = DateTime.Today.AddDays(-i);
                var nextDate = date.AddDays(1);
                
                // Count completed attempts for this day
                var completionCount = await _context.TestAttempts
                    .Where(a => a.TestId == testId 
                               && a.IsCompleted 
                               && a.EndTime.HasValue
                               && a.EndTime.Value.Date == date)
                    .CountAsync();
                    
                timelineData.Add(new DailyCompletionData
                {
                    Date = date,
                    CompletionCount = completionCount,
                    DateLabel = date.ToString("MMM dd")
                });
            }
            
            return timelineData;
        }
    }
}