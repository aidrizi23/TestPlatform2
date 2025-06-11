
using System.ComponentModel.DataAnnotations;

using TestPlatform2.Data;

namespace TestPlatform2.Models;

public record TestForCreationDto
{
    [Required(ErrorMessage = "Test name is required")]
    public string Title { get; init; } = "";

    public string Description { get; init; } = "";
    public bool RandomizeQuestions { get; init; } = false;

    [Required]
    [Range(1, 120, ErrorMessage = "Time limit must be between 1 and 120 minutes")]
    public int TimeLimit { get; init; } = 30;

    [Required]
    [Range(1, 10, ErrorMessage = "No more than 10 attempts allowed")]
    public int MaxAttempts { get; init; } = 1;

    public string? CategoryId { get; init; }
    
    public List<string> TagNames { get; init; } = new();
    
    // Scheduling properties
    public bool IsScheduled { get; init; } = false;
    public DateTime? ScheduledStartDate { get; init; }
    public DateTime? ScheduledEndDate { get; init; }
    public bool AutoPublish { get; init; } = false;
    public bool AutoClose { get; init; } = false;

}