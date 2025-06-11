using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace TestPlatform2.Data.Questions;

public class ImageBasedQuestion : Question
{
    // Main image URL for the question
    public string ImageUrl { get; set; } = "";
    
    // JSON serialized list of hotspots/clickable areas
    public string HotspotsJson { get; set; } = "[]";
    
    // Type of image-based question
    public ImageQuestionType QuestionType { get; set; } = ImageQuestionType.Hotspot;
    
    // For image labeling questions - JSON serialized list of labels
    public string LabelsJson { get; set; } = "[]";
    
    // Alternative text for accessibility
    public string? AltText { get; set; }
    
    // Image dimensions for consistent display
    public int ImageWidth { get; set; } = 800;
    public int ImageHeight { get; set; } = 600;

    // Not mapped properties for easy access
    [NotMapped]
    public List<ImageHotspot> Hotspots
    {
        get => string.IsNullOrEmpty(HotspotsJson) 
            ? new List<ImageHotspot>() 
            : JsonSerializer.Deserialize<List<ImageHotspot>>(HotspotsJson) ?? new List<ImageHotspot>();
        set => HotspotsJson = JsonSerializer.Serialize(value);
    }

    [NotMapped]
    public List<ImageLabel> Labels
    {
        get => string.IsNullOrEmpty(LabelsJson) 
            ? new List<ImageLabel>() 
            : JsonSerializer.Deserialize<List<ImageLabel>>(LabelsJson) ?? new List<ImageLabel>();
        set => LabelsJson = JsonSerializer.Serialize(value);
    }

    public override bool ValidateAnswer(object answer)
    {
        if (answer is not string answerJson) return false;
        
        try
        {
            switch (QuestionType)
            {
                case ImageQuestionType.Hotspot:
                    return ValidateHotspotAnswer(answerJson);
                case ImageQuestionType.Labeling:
                    return ValidateLabelingAnswer(answerJson);
                case ImageQuestionType.ClickSequence:
                    return ValidateSequenceAnswer(answerJson);
                default:
                    return false;
            }
        }
        catch
        {
            return false;
        }
    }

    private bool ValidateHotspotAnswer(string answerJson)
    {
        var studentClicks = JsonSerializer.Deserialize<List<ClickPoint>>(answerJson);
        if (studentClicks == null) return false;

        var correctHotspots = Hotspots.Where(h => h.IsCorrect).ToList();
        
        // Check if student clicked on all correct hotspots
        foreach (var hotspot in correctHotspots)
        {
            bool foundClick = studentClicks.Any(click => IsPointInHotspot(click, hotspot));
            if (!foundClick) return false;
        }

        // Check if student didn't click on incorrect hotspots
        var incorrectHotspots = Hotspots.Where(h => !h.IsCorrect).ToList();
        foreach (var click in studentClicks)
        {
            bool clickedIncorrect = incorrectHotspots.Any(hotspot => IsPointInHotspot(click, hotspot));
            if (clickedIncorrect) return false;
        }

        return true;
    }

    private bool ValidateLabelingAnswer(string answerJson)
    {
        var studentLabels = JsonSerializer.Deserialize<Dictionary<string, string>>(answerJson);
        if (studentLabels == null) return false;

        var correctLabels = Labels.ToDictionary(l => l.Id, l => l.CorrectText);
        
        return correctLabels.All(kvp => 
            studentLabels.ContainsKey(kvp.Key) && 
            string.Equals(studentLabels[kvp.Key].Trim(), kvp.Value.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    private bool ValidateSequenceAnswer(string answerJson)
    {
        var studentSequence = JsonSerializer.Deserialize<List<ClickPoint>>(answerJson);
        if (studentSequence == null) return false;

        var correctSequence = Hotspots.Where(h => h.IsCorrect).OrderBy(h => h.SequenceOrder).ToList();
        
        if (studentSequence.Count != correctSequence.Count) return false;

        for (int i = 0; i < correctSequence.Count; i++)
        {
            if (!IsPointInHotspot(studentSequence[i], correctSequence[i]))
                return false;
        }

        return true;
    }

    private bool IsPointInHotspot(ClickPoint click, ImageHotspot hotspot)
    {
        return click.X >= hotspot.X && click.X <= (hotspot.X + hotspot.Width) &&
               click.Y >= hotspot.Y && click.Y <= (hotspot.Y + hotspot.Height);
    }
}

public enum ImageQuestionType
{
    Hotspot = 0,        // Click on correct areas
    Labeling = 1,       // Add labels to specific points
    ClickSequence = 2   // Click areas in correct order
}

public class ImageHotspot
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Label { get; set; } = "";
    public double X { get; set; } = 0;
    public double Y { get; set; } = 0;
    public double Width { get; set; } = 50;
    public double Height { get; set; } = 50;
    public bool IsCorrect { get; set; } = true;
    public int SequenceOrder { get; set; } = 0; // For sequence questions
    public string? Feedback { get; set; }
}

public class ImageLabel
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public double X { get; set; } = 0;
    public double Y { get; set; } = 0;
    public string CorrectText { get; set; } = "";
    public string? Hint { get; set; }
}

public class ClickPoint
{
    public double X { get; set; }
    public double Y { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}