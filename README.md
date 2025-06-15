# TestPlatform3 - Comprehensive Test Management System

A modern, full-featured test/quiz platform built with **ASP.NET Core 8.0** that enables educators to create, manage, and analyze online tests with comprehensive student management capabilities.

## 🚀 Features

### Core Features
- **Multi-format Questions**: True/False, Multiple Choice, Short Answer with rich text support
- **Intelligent Grading**: Automatic grading with manual override capabilities
- **Email Invitations**: Token-based student access with unique links
- **Real-time Analytics**: Comprehensive test performance metrics and reporting
- **Export Capabilities**: PDF and Excel exports for results and analytics
- **Test Scheduling**: Automatic test publishing and closing with time-based controls
- **Responsive Design**: Mobile-friendly interface with Bootstrap 5

### Advanced Features
- **Test Randomization**: Shuffle questions for academic integrity
- **Time Limits**: Configurable test duration with automatic submission
- **Attempt Limits**: Control number of test attempts per student
- **Question Protection**: Prevents modification of questions after student submissions
- **Bulk Operations**: Mass manage tests, questions, and results
- **Category Management**: Organize tests by subject or topic
- **Tag System**: Flexible test organization and filtering

## 🏗️ Architecture

### Technology Stack
- **Backend**: ASP.NET Core 8.0 MVC
- **Database**: PostgreSQL with Entity Framework Core
- **Authentication**: ASP.NET Core Identity
- **Frontend**: Bootstrap 5, jQuery, AJAX
- **Payment**: Stripe Integration
- **Email**: SMTP with HTML templates
- **Export**: iTextSharp (PDF), EPPlus (Excel)
- **Image Processing**: SixLabors.ImageSharp

### Design Patterns
- **Repository Pattern**: Clean data access abstraction
- **MVC Architecture**: Clear separation of concerns
- **Dependency Injection**: Built-in ASP.NET Core DI container
- **Table Per Hierarchy**: Question type inheritance
- **Token-based Access**: Secure student authentication

## 📁 Project Structure

```
TestPlatform3/
├── Controllers/           # MVC Controllers
│   ├── TestController.cs       # Main test management
│   ├── QuestionController.cs   # Question CRUD operations
│   ├── TestAttemptController.cs # Student experience
│   └── TestInviteController.cs # Email invitations
├── Data/                 # Entity Models & DbContext
│   ├── ApplicationDbContext.cs
│   ├── User.cs                 # Extended Identity user
│   ├── Test.cs                 # Main test entity
│   ├── TestAttempt.cs          # Student submissions
│   └── Questions/              # Question inheritance
├── Repository/           # Data access layer
│   ├── Interfaces/
│   └── Implementations/
├── Services/             # Business logic
│   ├── EmailService.cs         # SMTP email handling
│   ├── ExportService.cs        # PDF/Excel generation
│   └── TestSchedulingService.cs # Background scheduling
├── Models/               # ViewModels & DTOs
├── Views/                # Razor templates
│   ├── Test/                   # Test management views
│   ├── TestAttempt/            # Student interface
│   └── Question/               # Question creation forms
└── wwwroot/              # Static assets
    ├── css/                    # Custom stylesheets
    └── js/                     # JavaScript files
```

## 🗄️ Database Schema

### Core Entities

#### Test Entity
```csharp
public class Test
{
    public string Id { get; set; }
    public string TestName { get; set; }
    public string Description { get; set; }
    public bool RandomizeQuestions { get; set; }
    public int TimeLimit { get; set; }
    public int MaxAttempts { get; set; }
    public bool IsLocked { get; set; }
    public bool IsArchived { get; set; }
    
    // Scheduling
    public bool IsScheduled { get; set; }
    public DateTime? ScheduledStartDate { get; set; }
    public DateTime? ScheduledEndDate { get; set; }
    
    // Relationships
    public string UserId { get; set; }
    public User User { get; set; }
    public virtual ICollection<Question> Questions { get; set; }
    public List<TestInvite> InvitedStudents { get; set; }
    public List<TestAttempt> Attempts { get; set; }
}
```

#### Question Hierarchy (TPH Inheritance)
```csharp
// Base class
public abstract class Question
{
    public string Id { get; set; }
    public string Text { get; set; }
    public double Points { get; set; }
    public int Position { get; set; }
    public string TestId { get; set; }
    public Test Test { get; set; }
}

// Derived types
public class TrueFalseQuestion : Question
{
    public bool CorrectAnswer { get; set; }
}

public class MultipleChoiceQuestion : Question
{
    public List<string> Options { get; set; }
    public List<string> CorrectAnswers { get; set; }
}

public class ShortAnswerQuestion : Question
{
    public string ExpectedAnswer { get; set; }
    public bool CaseSensitive { get; set; }
}
```

## 🚦 Getting Started

### Prerequisites
- .NET 8.0 SDK
- PostgreSQL 13+
- Visual Studio 2022 or JetBrains Rider

### Installation

1. **Clone the repository**
   ```bash
   git clone [repository-url]
   cd TestPlatform3
   ```

2. **Set up PostgreSQL**
   - Install PostgreSQL and create a database
   - Update connection string in `appsettings.json`

3. **Configure settings**
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Host=localhost;Database=testplatform;Username=postgres;Password=yourpassword"
     },
     "Smtp": {
       "Host": "smtp.gmail.com",
       "Port": 587,
       "Username": "your-email@gmail.com",
       "Password": "your-app-password"
     }
   }
   ```

4. **Run database migrations**
   ```bash
   dotnet ef database update
   ```

5. **Start the application**
   ```bash
   dotnet run
   ```

### Development Commands

```bash
# Restore dependencies and build
dotnet restore
dotnet build

# Run with hot reload
dotnet watch run

# Database operations
dotnet ef migrations add MigrationName
dotnet ef database update
dotnet ef database drop  # BE CAREFUL
```

## 🎯 Key Workflows

### Creating and Managing Tests

1. **Test Creation**
   - Navigate to Tests → Create New Test
   - Fill in test details (name, description, settings)
   - Add questions using the question builder
   - Configure time limits and attempt restrictions

2. **Question Management**
   - Add multiple question types to tests
   - Set point values and correct answers
   - Reorder questions with drag-and-drop
   - Preview questions before publishing

3. **Student Invitations**
   - Send email invitations with unique tokens
   - Monitor invitation status and usage
   - Resend invitations if needed

4. **Results Analysis**
   - View detailed analytics and performance metrics
   - Export results to PDF or Excel
   - Grade short answer questions manually
   - Adjust point values as needed

### Student Experience

1. **Access Test**
   - Receive email invitation with unique link
   - Enter token if accessing manually
   - Provide student information

2. **Take Test**
   - Answer questions in sequence
   - Navigate between questions
   - Submit when complete or time expires

3. **View Results**
   - See immediate results for auto-graded questions
   - Await manual grading for short answers

## 🔧 Configuration

### Email Configuration (SMTP)
```json
{
  "Smtp": {
    "Host": "smtp.gmail.com",
    "Port": 587,
    "Username": "your-email@gmail.com",
    "Password": "your-app-password",
    "EnableSsl": true,
    "FromEmail": "your-email@gmail.com",
    "FromName": "Test Platform"
  }
}
```

### Database Configuration
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=testplatform;Username=postgres;Password=yourpassword"
  }
}
```

### Stripe Configuration (Optional)
```json
{
  "Stripe": {
    "PublishableKey": "pk_test_...",
    "SecretKey": "sk_test_...",
    "WebhookSecret": "whsec_..."
  }
}
```

## 🛡️ Security Features

- **ASP.NET Core Identity**: User authentication and authorization
- **Token-based Access**: Secure student test access without registration
- **Data Ownership**: Users can only access their own tests and data
- **CSRF Protection**: Automatic protection on state-changing operations
- **SQL Injection Protection**: Entity Framework parameterized queries
- **XSS Prevention**: Automatic HTML encoding in Razor views

## 📊 Analytics & Reporting

### Test Analytics
- Overall test performance metrics
- Question-by-question analysis
- Student performance distribution
- Time-based completion analytics

### Export Options
- **PDF Reports**: Professional test results and analytics
- **Excel Exports**: Detailed data for further analysis
- **Custom Formatting**: Branded reports with institutional logos

## 🔄 API Endpoints

### AJAX Endpoints for Dynamic UI
- `POST /Test/CreateAjax` - Create test via AJAX
- `POST /Test/EditAjax` - Update test details
- `POST /Test/DeleteAjax` - Delete test
- `POST /Test/LockTestAjax` - Lock/unlock test
- `GET /Test/GetTestStatisticsAjax` - Real-time statistics

### Question Management
- `POST /Question/CreateTrueFalseAjax`
- `POST /Question/CreateMultipleChoiceAjax`
- `POST /Question/CreateShortAnswerAjax`
- `POST /Question/DeleteAjax`

## 🧪 Testing

### Running Tests
```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

### Test Structure
- Unit tests for business logic
- Integration tests for controllers
- Repository tests for data access
- Service tests for email and export functionality

## 🚀 Deployment

### Production Considerations
- Use production PostgreSQL instance
- Configure proper SMTP settings
- Set up SSL certificates
- Configure logging and monitoring
- Set up backup procedures

### Docker Deployment
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
# Build steps...
```

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add some amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## 📝 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🆘 Support

For support and questions:
- Check the documentation in `/docs`
- Review the [CLAUDE.md](CLAUDE.md) file for development guidelines
- Open an issue for bug reports or feature requests

## 🔮 Roadmap

- [ ] Real-time collaborative test editing
- [ ] Advanced question types (matching, drag-and-drop)
- [ ] Plagiarism detection integration
- [ ] Mobile app for test taking
- [ ] API for third-party integrations
- [ ] Advanced analytics with ML insights

---

Built with ❤️ using ASP.NET Core 8.0