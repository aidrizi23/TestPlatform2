using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace TestPlatform2.Data.Questions;

public class DragDropQuestion : Question
{
    // JSON serialized list of draggable items
    public string DraggableItemsJson { get; set; } = "[]";
    
    // JSON serialized list of drop zones with correct answers
    public string DropZonesJson { get; set; } = "[]";
    
    // Whether to allow multiple items in same drop zone
    public bool AllowMultiplePerZone { get; set; } = false;
    
    // Whether order matters within drop zones
    public bool OrderMatters { get; set; } = false;

    // Not mapped properties for easy access
    [NotMapped]
    public List<DragDropItem> DraggableItems
    {
        get => string.IsNullOrEmpty(DraggableItemsJson) 
            ? new List<DragDropItem>() 
            : JsonSerializer.Deserialize<List<DragDropItem>>(DraggableItemsJson) ?? new List<DragDropItem>();
        set => DraggableItemsJson = JsonSerializer.Serialize(value);
    }

    [NotMapped]
    public List<DropZone> DropZones
    {
        get => string.IsNullOrEmpty(DropZonesJson) 
            ? new List<DropZone>() 
            : JsonSerializer.Deserialize<List<DropZone>>(DropZonesJson) ?? new List<DropZone>();
        set => DropZonesJson = JsonSerializer.Serialize(value);
    }

    public override bool ValidateAnswer(object answer)
    {
        if (answer is not string answerJson) return false;
        
        try
        {
            var studentAnswer = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(answerJson);
            if (studentAnswer == null) return false;

            var dropZones = DropZones;
            bool allCorrect = true;

            foreach (var zone in dropZones)
            {
                var studentItems = studentAnswer.GetValueOrDefault(zone.Id, new List<string>());
                var correctItems = zone.CorrectItems;

                if (OrderMatters)
                {
                    // Check exact order match
                    if (!studentItems.SequenceEqual(correctItems))
                    {
                        allCorrect = false;
                        break;
                    }
                }
                else
                {
                    // Check if all correct items are present (order doesn't matter)
                    if (!correctItems.All(item => studentItems.Contains(item)) ||
                        studentItems.Count != correctItems.Count)
                    {
                        allCorrect = false;
                        break;
                    }
                }
            }

            return allCorrect;
        }
        catch
        {
            return false;
        }
    }
}

public class DragDropItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Text { get; set; } = "";
    public string? ImageUrl { get; set; }
    public string? Tooltip { get; set; }
}

public class DropZone
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Label { get; set; } = "";
    public string? Description { get; set; }
    public List<string> CorrectItems { get; set; } = new();
    public int MaxItems { get; set; } = 1;
    public double X { get; set; } = 0; // Position for visual layout
    public double Y { get; set; } = 0;
    public double Width { get; set; } = 200;
    public double Height { get; set; } = 100;
}