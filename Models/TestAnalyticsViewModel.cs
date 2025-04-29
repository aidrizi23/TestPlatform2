using System;
using System.Collections.Generic;
using TestPlatform2.Data;
using TestPlatform2.Data.Questions;

namespace TestPlatform2.Models
{
    public class TestAnalyticsViewModel
    {
        // Test Information
        public string TestId { get; set; }
        public string TestName { get; set; }
        public string Description { get; set; }
        public int TotalAttempts { get; set; }
        public int CompletedAttempts { get; set; }
        public int InProgressAttempts { get; set; }
        
        // Overall Performance Metrics
        public double AverageScore { get; set; }
        public double MedianScore { get; set; }
        public double HighestScore { get; set; }
        public double LowestScore { get; set; }
        public double StandardDeviation { get; set; }
        public double PassingRate { get; set; } // Percentage of students who scored above 60%
        
        // Time Metrics
        public TimeSpan AverageCompletionTime { get; set; }
        public TimeSpan FastestCompletionTime { get; set; }
        public TimeSpan SlowestCompletionTime { get; set; }
        
        // Score Distribution
        public List<int> ScoreDistribution { get; set; } // Count of scores in ranges (0-10, 11-20, etc.)
        public List<string> ScoreRanges { get; set; } // Labels for score ranges
        
        // Question Performance
        public List<QuestionAnalyticsData> QuestionPerformance { get; set; }
        
        // Student Performance
        public List<StudentPerformanceData> TopPerformers { get; set; }
        public List<StudentPerformanceData> StrugglingSudents { get; set; }
        
        // Recent Attempts
        public List<RecentAttemptData> RecentAttempts { get; set; }
    }
    
    public class QuestionAnalyticsData
    {
        public string QuestionId { get; set; }
        public string QuestionText { get; set; }
        public string QuestionType { get; set; }
        public int Position { get; set; }
        public double Points { get; set; }
        public double AveragePoints { get; set; }
        public double SuccessRate { get; set; } // Percentage of correct answers
        public int CorrectAnswers { get; set; }
        public int IncorrectAnswers { get; set; }
        public double AverageTimeSpent { get; set; } // If tracked
        public Dictionary<string, int> AnswerDistribution { get; set; } // For multiple choice
    }
    
    public class StudentPerformanceData
    {
        public string AttemptId { get; set; }
        public string StudentName { get; set; }
        public string StudentEmail { get; set; }
        public double Score { get; set; }
        public double ScorePercentage { get; set; }
        public TimeSpan CompletionTime { get; set; }
        public DateTime CompletionDate { get; set; }
    }
    
    public class RecentAttemptData
    {
        public string AttemptId { get; set; }
        public string StudentName { get; set; }
        public string StudentEmail { get; set; }
        public double Score { get; set; }
        public double ScorePercentage { get; set; }
        public DateTime CompletionDate { get; set; }
        public bool IsCompleted { get; set; }
    }
    
    public class QuestionAnalyticsViewModel
    {
        public string TestId { get; set; }
        public string TestName { get; set; }
        public Question Question { get; set; }
        public QuestionAnalyticsData Analytics { get; set; }
    }   
}