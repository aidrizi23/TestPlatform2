using iTextSharp.text;
using iTextSharp.text.pdf;
using OfficeOpenXml;
using TestPlatform2.Data;
using TestPlatform2.Models;
using TestPlatform2.Controllers;
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

    public async Task<byte[]> ExportAnalyticsToPdfAsync(TestAnalyticsViewModel analyticsData, TestController.AnalyticsExportRequest request)
    {
        using var memoryStream = new MemoryStream();
        var document = new Document(PageSize.A4, 40, 40, 40, 40);
        var writer = PdfWriter.GetInstance(document, memoryStream);
        
        document.Open();

        // Title
        var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 18, BaseColor.DarkGray);
        var title = new Paragraph($"Analytics Report: {analyticsData.TestName}", titleFont);
        title.Alignment = Element.ALIGN_CENTER;
        title.SpacingAfter = 20f;
        document.Add(title);

        // Summary Statistics
        var headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12, BaseColor.Black);
        var bodyFont = FontFactory.GetFont(FontFactory.HELVETICA, 10, BaseColor.Black);

        document.Add(new Paragraph("Summary Statistics", headerFont) { SpacingAfter = 10f });
        
        var summaryTable = new PdfPTable(2) { WidthPercentage = 100 };
        summaryTable.SetWidths(new float[] { 1, 1 });
        
        AddTableRow(summaryTable, "Total Attempts:", analyticsData.TotalAttempts.ToString(), headerFont, bodyFont);
        AddTableRow(summaryTable, "Completed Attempts:", analyticsData.CompletedAttempts.ToString(), headerFont, bodyFont);
        AddTableRow(summaryTable, "Average Score:", $"{analyticsData.AverageScore:F1}", headerFont, bodyFont);
        AddTableRow(summaryTable, "Pass Rate:", $"{analyticsData.PassingRate:F1}%", headerFont, bodyFont);
        AddTableRow(summaryTable, "Median Score:", $"{analyticsData.MedianScore:F1}", headerFont, bodyFont);
        AddTableRow(summaryTable, "Standard Deviation:", $"{analyticsData.StandardDeviation:F2}", headerFont, bodyFont);
        
        document.Add(summaryTable);
        document.Add(new Paragraph(" ") { SpacingAfter = 20f });

        // Question Performance
        if (request.IncludeInsights && analyticsData.QuestionPerformance?.Any() == true)
        {
            document.Add(new Paragraph("Question Performance", headerFont) { SpacingAfter = 10f });
            
            var questionTable = new PdfPTable(5) { WidthPercentage = 100 };
            questionTable.SetWidths(new float[] { 1, 2, 1, 1, 1 });
            
            // Headers
            AddHeaderCell(questionTable, "Q#", headerFont, BaseColor.LightGray);
            AddHeaderCell(questionTable, "Type", headerFont, BaseColor.LightGray);
            AddHeaderCell(questionTable, "Points", headerFont, BaseColor.LightGray);
            AddHeaderCell(questionTable, "Success Rate", headerFont, BaseColor.LightGray);
            AddHeaderCell(questionTable, "Responses", headerFont, BaseColor.LightGray);
            
            foreach (var question in analyticsData.QuestionPerformance.Take(20)) // Limit for PDF
            {
                questionTable.AddCell(new Phrase($"Q{question.Position + 1}", bodyFont));
                questionTable.AddCell(new Phrase(question.QuestionType, bodyFont));
                questionTable.AddCell(new Phrase(question.Points.ToString(), bodyFont));
                questionTable.AddCell(new Phrase($"{question.SuccessRate:F1}%", bodyFont));
                questionTable.AddCell(new Phrase((question.CorrectAnswers + question.IncorrectAnswers).ToString(), bodyFont));
            }
            
            document.Add(questionTable);
        }

        document.Close();
        return memoryStream.ToArray();
    }

    public async Task<byte[]> ExportAnalyticsToExcelAsync(TestAnalyticsViewModel analyticsData, TestController.AnalyticsExportRequest request)
    {
        using var package = new ExcelPackage();
        
        // Summary worksheet
        var summaryWorksheet = package.Workbook.Worksheets.Add("Summary");
        
        summaryWorksheet.Cells[1, 1].Value = "Analytics Report";
        summaryWorksheet.Cells[1, 1, 1, 4].Merge = true;
        summaryWorksheet.Cells[1, 1].Style.Font.Size = 16;
        summaryWorksheet.Cells[1, 1].Style.Font.Bold = true;
        
        summaryWorksheet.Cells[2, 1].Value = "Test Name:";
        summaryWorksheet.Cells[2, 2].Value = analyticsData.TestName;
        
        var row = 4;
        summaryWorksheet.Cells[row, 1].Value = "Total Attempts";
        summaryWorksheet.Cells[row, 2].Value = analyticsData.TotalAttempts;
        row++;
        
        summaryWorksheet.Cells[row, 1].Value = "Completed Attempts";
        summaryWorksheet.Cells[row, 2].Value = analyticsData.CompletedAttempts;
        row++;
        
        summaryWorksheet.Cells[row, 1].Value = "Average Score";
        summaryWorksheet.Cells[row, 2].Value = analyticsData.AverageScore;
        row++;
        
        summaryWorksheet.Cells[row, 1].Value = "Pass Rate (%)";
        summaryWorksheet.Cells[row, 2].Value = analyticsData.PassingRate;
        row++;
        
        summaryWorksheet.Cells[row, 1].Value = "Median Score";
        summaryWorksheet.Cells[row, 2].Value = analyticsData.MedianScore;
        row++;
        
        summaryWorksheet.Cells[row, 1].Value = "Standard Deviation";
        summaryWorksheet.Cells[row, 2].Value = analyticsData.StandardDeviation;
        
        // Question Performance worksheet
        if (analyticsData.QuestionPerformance?.Any() == true)
        {
            var questionWorksheet = package.Workbook.Worksheets.Add("Question Performance");
            
            questionWorksheet.Cells[1, 1].Value = "Question #";
            questionWorksheet.Cells[1, 2].Value = "Question Text";
            questionWorksheet.Cells[1, 3].Value = "Type";
            questionWorksheet.Cells[1, 4].Value = "Points";
            questionWorksheet.Cells[1, 5].Value = "Success Rate (%)";
            questionWorksheet.Cells[1, 6].Value = "Correct Answers";
            questionWorksheet.Cells[1, 7].Value = "Incorrect Answers";
            questionWorksheet.Cells[1, 8].Value = "Average Points";
            
            // Make headers bold
            questionWorksheet.Cells[1, 1, 1, 8].Style.Font.Bold = true;
            
            row = 2;
            foreach (var question in analyticsData.QuestionPerformance)
            {
                questionWorksheet.Cells[row, 1].Value = $"Q{question.Position + 1}";
                questionWorksheet.Cells[row, 2].Value = question.QuestionText;
                questionWorksheet.Cells[row, 3].Value = question.QuestionType;
                questionWorksheet.Cells[row, 4].Value = question.Points;
                questionWorksheet.Cells[row, 5].Value = question.SuccessRate;
                questionWorksheet.Cells[row, 6].Value = question.CorrectAnswers;
                questionWorksheet.Cells[row, 7].Value = question.IncorrectAnswers;
                questionWorksheet.Cells[row, 8].Value = question.AveragePoints;
                row++;
            }
            
            questionWorksheet.Cells.AutoFitColumns();
        }

        // Student Performance worksheet
        if (request.IncludeStudentData && analyticsData.TopPerformers?.Any() == true)
        {
            var studentWorksheet = package.Workbook.Worksheets.Add("Student Performance");
            
            studentWorksheet.Cells[1, 1].Value = "Student Name";
            studentWorksheet.Cells[1, 2].Value = "Email";
            studentWorksheet.Cells[1, 3].Value = "Score";
            studentWorksheet.Cells[1, 4].Value = "Score Percentage";
            studentWorksheet.Cells[1, 5].Value = "Completion Time";
            studentWorksheet.Cells[1, 6].Value = "Completion Date";
            
            // Make headers bold
            studentWorksheet.Cells[1, 1, 1, 6].Style.Font.Bold = true;
            
            row = 2;
            var allStudents = analyticsData.TopPerformers.Concat(analyticsData.StrugglingSudents ?? new List<StudentPerformanceData>());
            foreach (var student in allStudents)
            {
                studentWorksheet.Cells[row, 1].Value = student.StudentName;
                studentWorksheet.Cells[row, 2].Value = student.StudentEmail;
                studentWorksheet.Cells[row, 3].Value = student.Score;
                studentWorksheet.Cells[row, 4].Value = student.ScorePercentage;
                studentWorksheet.Cells[row, 5].Value = student.CompletionTime.ToString(@"hh\:mm\:ss");
                studentWorksheet.Cells[row, 6].Value = student.CompletionDate;
                row++;
            }
            
            studentWorksheet.Cells.AutoFitColumns();
        }
        
        summaryWorksheet.Cells.AutoFitColumns();
        
        return package.GetAsByteArray();
    }

    public async Task<byte[]> ExportAnalyticsToCsvAsync(TestAnalyticsViewModel analyticsData, TestController.AnalyticsExportRequest request)
    {
        var csv = new StringBuilder();
        
        // Add summary data
        csv.AppendLine("Analytics Summary");
        csv.AppendLine($"Test Name,{analyticsData.TestName}");
        csv.AppendLine($"Total Attempts,{analyticsData.TotalAttempts}");
        csv.AppendLine($"Completed Attempts,{analyticsData.CompletedAttempts}");
        csv.AppendLine($"Average Score,{analyticsData.AverageScore:F1}");
        csv.AppendLine($"Pass Rate (%),{analyticsData.PassingRate:F1}");
        csv.AppendLine($"Median Score,{analyticsData.MedianScore:F1}");
        csv.AppendLine($"Standard Deviation,{analyticsData.StandardDeviation:F2}");
        csv.AppendLine();
        
        // Add question performance data
        if (analyticsData.QuestionPerformance?.Any() == true)
        {
            csv.AppendLine("Question Performance");
            csv.AppendLine("Question #,Question Text,Type,Points,Success Rate (%),Correct Answers,Incorrect Answers,Average Points");
            
            foreach (var question in analyticsData.QuestionPerformance)
            {
                csv.AppendLine($"Q{question.Position + 1},\"{question.QuestionText.Replace("\"", "\"\"")}\",{question.QuestionType},{question.Points},{question.SuccessRate:F1},{question.CorrectAnswers},{question.IncorrectAnswers},{question.AveragePoints:F2}");
            }
            csv.AppendLine();
        }
        
        // Add student performance data if requested
        if (request.IncludeStudentData && analyticsData.TopPerformers?.Any() == true)
        {
            csv.AppendLine("Student Performance");
            csv.AppendLine("Student Name,Email,Score,Score Percentage,Completion Time,Completion Date");
            
            var allStudents = analyticsData.TopPerformers.Concat(analyticsData.StrugglingSudents ?? new List<StudentPerformanceData>());
            foreach (var student in allStudents)
            {
                csv.AppendLine($"\"{student.StudentName}\",{student.StudentEmail},{student.Score},{student.ScorePercentage:F1},{student.CompletionTime:hh\\:mm\\:ss},{student.CompletionDate:yyyy-MM-dd HH:mm:ss}");
            }
        }
        
        return Encoding.UTF8.GetBytes(csv.ToString());
    }
}