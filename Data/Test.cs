﻿using TestPlatform2.Data.Questions;

namespace TestPlatform2.Data;

public class Test
{

    public string Id { get; set; } = Guid.NewGuid().ToString();

    public required string TestName { get; set; }
    public string Description { get; set; }
    public bool RandomizeQuestions { get; set; }
    public int TimeLimit { get; set; }
    public int MaxAttempts { get; set; }

    // Correctly map the foreign key
    public string UserId { get; set; }
    public User User { get; set; }

    public virtual ICollection<Question> Questions { get; set; } = new List<Question>();
    
    public List<TestInvite> InvitedStudents { get; set; } = new();
    public List<TestAttempt> Attempts { get; set; } = new();
    public bool IsLocked { get; set; } = false; // Default values
    public bool IsArchived { get; set; } = false;
    public DateTime? ArchivedAt { get; set; }
    
    // Category and Tags
    public string? CategoryId { get; set; }
    public TestCategory? Category { get; set; }
    
    public virtual ICollection<TestTag> Tags { get; set; } = new List<TestTag>();
    
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    
    // Scheduling properties
    public bool IsScheduled { get; set; } = false;
    public DateTime? ScheduledStartDate { get; set; }
    public DateTime? ScheduledEndDate { get; set; }
    public TestStatus Status { get; set; } = TestStatus.Draft;
    public bool AutoPublish { get; set; } = false;
    public bool AutoClose { get; set; } = false;
}

public enum TestStatus
{
    Draft = 0,
    Scheduled = 1,
    Active = 2,
    Closed = 3,
    Archived = 4
}
