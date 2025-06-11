using iTextSharp.text;
using iTextSharp.text.pdf;
using OfficeOpenXml;
using TestPlatform2.Data;
using System.Text;

namespace TestPlatform2.Services;

public class ExportService : IExportService
{
    public async Task<byte[]> ExportTestResultsToPdfAsync(Test test, IEnumerable<TestAttempt> attempts)
    {
        using var memoryStream = new MemoryStream();
        var document = new Document(PageSize.A4, 40, 40, 40, 40);
        var writer = PdfWriter.GetInstance(document, memoryStream);
        
        document.Open();

        // Title
        var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 18, BaseColor.DarkGray);
        var title = new Paragraph($"Test Results: {test.TestName}", titleFont);
        title.Alignment = Element.ALIGN_CENTER;
        title.SpacingAfter = 20f;
        document.Add(title);

        // Test Information
        var headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12, BaseColor.Black);
        var bodyFont = FontFactory.GetFont(FontFactory.HELVETICA, 10, BaseColor.Black);

        document.Add(new Paragraph("Test Information", headerFont) { SpacingAfter = 10f });
        
        var testInfoTable = new PdfPTable(2) { WidthPercentage = 100 };
        testInfoTable.SetWidths(new float[] { 1, 2 });
        
        AddTableRow(testInfoTable, "Test Name:", test.TestName, headerFont, bodyFont);
        AddTableRow(testInfoTable, "Description:", test.Description ?? "No description", headerFont, bodyFont);
        AddTableRow(testInfoTable, "Time Limit:", $"{test.TimeLimit} minutes", headerFont, bodyFont);
        AddTableRow(testInfoTable, "Total Questions:", test.Questions?.Count.ToString() ?? "0", headerFont, bodyFont);
        AddTableRow(testInfoTable, "Total Attempts:", attempts.Count().ToString(), headerFont, bodyFont);
        AddTableRow(testInfoTable, "Completed Attempts:", attempts.Count(a => a.IsCompleted).ToString(), headerFont, bodyFont);
        
        if (attempts.Any(a => a.IsCompleted))
        {
            var avgScore = attempts.Where(a => a.IsCompleted).Average(a => a.Score);
            AddTableRow(testInfoTable, "Average Score:", $"{avgScore:F1}%", headerFont, bodyFont);
        }
        
        document.Add(testInfoTable);
        document.Add(new Paragraph(" ") { SpacingAfter = 20f });

        // Results Table
        document.Add(new Paragraph("Detailed Results", headerFont) { SpacingAfter = 10f });
        
        var resultsTable = new PdfPTable(6) { WidthPercentage = 100 };
        resultsTable.SetWidths(new float[] { 2, 2, 1.5f, 1.5f, 1, 1 });
        
        // Table Headers
        var headerBgColor = new BaseColor(240, 240, 240);
        AddHeaderCell(resultsTable, "Student Name", headerFont, headerBgColor);
        AddHeaderCell(resultsTable, "Email", headerFont, headerBgColor);
        AddHeaderCell(resultsTable, "Start Time", headerFont, headerBgColor);
        AddHeaderCell(resultsTable, "End Time", headerFont, headerBgColor);
        AddHeaderCell(resultsTable, "Status", headerFont, headerBgColor);
        AddHeaderCell(resultsTable, "Score", headerFont, headerBgColor);

        // Data Rows
        foreach (var attempt in attempts.OrderByDescending(a => a.StartTime))
        {
            resultsTable.AddCell(new PdfPCell(new Phrase($"{attempt.FirstName} {attempt.LastName}", bodyFont)) { Padding = 5 });
            resultsTable.AddCell(new PdfPCell(new Phrase(attempt.StudentEmail, bodyFont)) { Padding = 5 });
            resultsTable.AddCell(new PdfPCell(new Phrase(attempt.StartTime.ToString("MM/dd/yyyy HH:mm"), bodyFont)) { Padding = 5 });
            resultsTable.AddCell(new PdfPCell(new Phrase(attempt.EndTime?.ToString("MM/dd/yyyy HH:mm") ?? "-", bodyFont)) { Padding = 5 });
            resultsTable.AddCell(new PdfPCell(new Phrase(attempt.IsCompleted ? "Completed" : "In Progress", bodyFont)) { Padding = 5 });
            resultsTable.AddCell(new PdfPCell(new Phrase(attempt.IsCompleted ? $"{attempt.Score:F1}%" : "-", bodyFont)) { Padding = 5 });
        }
        
        document.Add(resultsTable);

        // Footer
        document.Add(new Paragraph(" ") { SpacingAfter = 20f });
        var footer = new Paragraph($"Generated on {DateTime.Now:MM/dd/yyyy HH:mm}", 
            FontFactory.GetFont(FontFactory.HELVETICA, 8, BaseColor.Gray));
        footer.Alignment = Element.ALIGN_CENTER;
        document.Add(footer);

        document.Close();
        return memoryStream.ToArray();
    }

    public async Task<byte[]> ExportTestResultsToExcelAsync(Test test, IEnumerable<TestAttempt> attempts)
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        
        using var package = new ExcelPackage();
        var worksheet = package.Workbook.Worksheets.Add("Test Results");

        // Title
        worksheet.Cells["A1"].Value = $"Test Results: {test.TestName}";
        worksheet.Cells["A1:F1"].Merge = true;
        worksheet.Cells["A1"].Style.Font.Size = 16;
        worksheet.Cells["A1"].Style.Font.Bold = true;
        worksheet.Cells["A1"].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;

        // Test Information
        var row = 3;
        worksheet.Cells[$"A{row}"].Value = "Test Information";
        worksheet.Cells[$"A{row}:B{row}"].Merge = true;
        worksheet.Cells[$"A{row}"].Style.Font.Bold = true;
        row++;

        worksheet.Cells[$"A{row}"].Value = "Test Name:";
        worksheet.Cells[$"B{row}"].Value = test.TestName;
        row++;
        
        worksheet.Cells[$"A{row}"].Value = "Description:";
        worksheet.Cells[$"B{row}"].Value = test.Description ?? "No description";
        row++;
        
        worksheet.Cells[$"A{row}"].Value = "Time Limit:";
        worksheet.Cells[$"B{row}"].Value = $"{test.TimeLimit} minutes";
        row++;
        
        worksheet.Cells[$"A{row}"].Value = "Total Questions:";
        worksheet.Cells[$"B{row}"].Value = test.Questions?.Count ?? 0;
        row++;
        
        worksheet.Cells[$"A{row}"].Value = "Total Attempts:";
        worksheet.Cells[$"B{row}"].Value = attempts.Count();
        row++;
        
        worksheet.Cells[$"A{row}"].Value = "Completed Attempts:";
        worksheet.Cells[$"B{row}"].Value = attempts.Count(a => a.IsCompleted);
        row++;

        if (attempts.Any(a => a.IsCompleted))
        {
            worksheet.Cells[$"A{row}"].Value = "Average Score:";
            worksheet.Cells[$"B{row}"].Value = $"{attempts.Where(a => a.IsCompleted).Average(a => a.Score):F1}%";
            row++;
        }

        row += 2;

        // Results Headers
        worksheet.Cells[$"A{row}"].Value = "Student Name";
        worksheet.Cells[$"B{row}"].Value = "Email";
        worksheet.Cells[$"C{row}"].Value = "Start Time";
        worksheet.Cells[$"D{row}"].Value = "End Time";
        worksheet.Cells[$"E{row}"].Value = "Status";
        worksheet.Cells[$"F{row}"].Value = "Score";

        // Style headers
        using (var range = worksheet.Cells[$"A{row}:F{row}"])
        {
            range.Style.Font.Bold = true;
            range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
            range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
            range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
            range.Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin);
        }

        row++;

        // Data
        foreach (var attempt in attempts.OrderByDescending(a => a.StartTime))
        {
            worksheet.Cells[$"A{row}"].Value = $"{attempt.FirstName} {attempt.LastName}";
            worksheet.Cells[$"B{row}"].Value = attempt.StudentEmail;
            worksheet.Cells[$"C{row}"].Value = attempt.StartTime.ToString("MM/dd/yyyy HH:mm");
            worksheet.Cells[$"D{row}"].Value = attempt.EndTime?.ToString("MM/dd/yyyy HH:mm") ?? "-";
            worksheet.Cells[$"E{row}"].Value = attempt.IsCompleted ? "Completed" : "In Progress";
            worksheet.Cells[$"F{row}"].Value = attempt.IsCompleted ? $"{attempt.Score:F1}%" : "-";
            
            // Add borders
            using (var range = worksheet.Cells[$"A{row}:F{row}"])
            {
                range.Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin);
            }
            
            row++;
        }

        // Auto-fit columns
        worksheet.Cells.AutoFitColumns();

        return package.GetAsByteArray();
    }

    public async Task<byte[]> ExportDetailedTestAnalyticsToPdfAsync(Test test, object analyticsData)
    {
        using var memoryStream = new MemoryStream();
        var document = new Document(PageSize.A4, 40, 40, 40, 40);
        var writer = PdfWriter.GetInstance(document, memoryStream);
        
        document.Open();

        // Title
        var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 18, BaseColor.DarkGray);
        var title = new Paragraph($"Test Analytics: {test.TestName}", titleFont);
        title.Alignment = Element.ALIGN_CENTER;
        title.SpacingAfter = 20f;
        document.Add(title);

        // Add analytics content here based on the analyticsData structure
        var bodyFont = FontFactory.GetFont(FontFactory.HELVETICA, 12, BaseColor.Black);
        document.Add(new Paragraph("Detailed analytics report will be generated here.", bodyFont));

        document.Close();
        return memoryStream.ToArray();
    }

    public async Task<byte[]> ExportTestSummaryToPdfAsync(Test test)
    {
        using var memoryStream = new MemoryStream();
        var document = new Document(PageSize.A4, 40, 40, 40, 40);
        var writer = PdfWriter.GetInstance(document, memoryStream);
        
        document.Open();

        // Title
        var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 18, BaseColor.DarkGray);
        var title = new Paragraph($"Test Summary: {test.TestName}", titleFont);
        title.Alignment = Element.ALIGN_CENTER;
        title.SpacingAfter = 20f;
        document.Add(title);

        // Test Summary
        var headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12, BaseColor.Black);
        var bodyFont = FontFactory.GetFont(FontFactory.HELVETICA, 10, BaseColor.Black);

        var table = new PdfPTable(2) { WidthPercentage = 100 };
        table.SetWidths(new float[] { 1, 2 });

        AddTableRow(table, "Test Name:", test.TestName, headerFont, bodyFont);
        AddTableRow(table, "Description:", test.Description ?? "No description", headerFont, bodyFont);
        AddTableRow(table, "Time Limit:", $"{test.TimeLimit} minutes", headerFont, bodyFont);
        AddTableRow(table, "Max Attempts:", test.MaxAttempts.ToString(), headerFont, bodyFont);
        AddTableRow(table, "Total Questions:", test.Questions?.Count.ToString() ?? "0", headerFont, bodyFont);
        AddTableRow(table, "Randomized:", test.RandomizeQuestions ? "Yes" : "No", headerFont, bodyFont);
        AddTableRow(table, "Status:", test.IsLocked ? "Locked" : "Active", headerFont, bodyFont);

        document.Add(table);

        // Questions List
        if (test.Questions?.Any() == true)
        {
            document.Add(new Paragraph(" ") { SpacingAfter = 10f });
            document.Add(new Paragraph("Questions", headerFont) { SpacingAfter = 10f });

            foreach (var question in test.Questions.OrderBy(q => q.Position))
            {
                var questionPara = new Paragraph($"{question.Position}. {question.Text} ({question.Points} points)", bodyFont);
                questionPara.SpacingAfter = 5f;
                document.Add(questionPara);
            }
        }

        document.Close();
        return memoryStream.ToArray();
    }

    private void AddTableRow(PdfPTable table, string label, string value, Font labelFont, Font valueFont)
    {
        table.AddCell(new PdfPCell(new Phrase(label, labelFont)) { Padding = 5, Border = Rectangle.NO_BORDER });
        table.AddCell(new PdfPCell(new Phrase(value, valueFont)) { Padding = 5, Border = Rectangle.NO_BORDER });
    }

    private void AddHeaderCell(PdfPTable table, string text, Font font, BaseColor backgroundColor)
    {
        var cell = new PdfPCell(new Phrase(text, font))
        {
            Padding = 8,
            BackgroundColor = backgroundColor,
            HorizontalAlignment = Element.ALIGN_CENTER
        };
        table.AddCell(cell);
    }
}