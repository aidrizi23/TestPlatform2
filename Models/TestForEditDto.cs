﻿using System.ComponentModel.DataAnnotations;

namespace TestPlatform2.Models;

public class TestForEditDto
{
    public string Id { get; set; }
    
    [Required(ErrorMessage = "Test name is required")]
    [StringLength(100, ErrorMessage = "Test name cannot exceed 100 characters")]
    public string TestName { get; set; }

    public string Description { get; set; }

    public bool RandomizeQuestions { get; set; }

    [Required(ErrorMessage = "Time limit is required")]
    [Range(1, 300, ErrorMessage = "Time limit must be between 1-300 minutes")]
    public int TimeLimit { get; set; }

    [Required(ErrorMessage = "Max attempts is required")]
    [Range(1, 10, ErrorMessage = "Max attempts must be between 1-10")]
    public int MaxAttempts { get; set; }
    
    public bool IsLocked { get; set; }
    
    public string? CategoryId { get; set; }
    
    public List<string> TagNames { get; set; } = new();
    
    // Scheduling properties
    public bool IsScheduled { get; set; } = false;
    public DateTime? ScheduledStartDate { get; set; }
    public DateTime? ScheduledEndDate { get; set; }
    public bool AutoPublish { get; set; } = false;
    public bool AutoClose { get; set; } = false;

}