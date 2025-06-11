# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Development Commands

### Building and Running
```bash
# Restore dependencies and build
dotnet restore
dotnet build

# Run the application in development mode
dotnet run

# Watch for changes during development
dotnet watch run
```

### Database Operations
```bash
# Add new migration
dotnet ef migrations add MigrationName

# Update database with latest migrations
dotnet ef database update

# Drop database (BE CAREFUL - destructive)
dotnet ef database drop
```

### Entity Framework Specific
The application uses PostgreSQL as the primary database. Connection string is in `appsettings.json`. When working with migrations, ensure PostgreSQL server is running on localhost:5432.

## Architecture Overview

### Core Domain Model
This is a **test/quiz platform** where teachers create tests and students take them. The architecture follows **Repository Pattern** with **ASP.NET Core MVC**.

**Key Entity Relationships:**
- `User` (ASP.NET Core Identity) → owns multiple `Tests`
- `Test` → contains multiple `Questions` (polymorphic inheritance)
- `Test` → has multiple `TestInvites` (email invitations to students) 
- `Test` → tracks multiple `TestAttempts` (student submissions)
- `TestAttempt` → contains multiple `Answers` (student responses to questions)

### Question Type Inheritance
Questions use **Table Per Hierarchy (TPH)** inheritance:
- `Question` (abstract base class)
  - `MultipleChoiceQuestion` - with options and multiple correct answers
  - `TrueFalseQuestion` - simple boolean questions  
  - `ShortAnswerQuestion` - text-based responses with case sensitivity options

### Repository Pattern Implementation
All data access goes through repository interfaces:
- `ITestRepository` / `TestRepository`
- `IQuestionRepository` / `QuestionRepository` 
- `ITestAttemptRepository` / `TestAttemptRepository`
- `IAnswerRepository` / `AnswerRepository`
- `ITestInviteRepository` / `TestInviteRepository`
- `ISubscriptionRepository` / `SubscriptionRepository`

### Controller Architecture
- **TestController**: Manages test CRUD, both traditional MVC and AJAX endpoints
- **QuestionController**: Handles question creation/editing for different types
- **TestAttemptController**: Student test-taking flow and result viewing
- **SubscriptionController**: Stripe integration for premium features
- **TestInviteController**: Email invitation system for students

### Key Business Rules
1. **Test Locking**: Tests can be locked/unlocked to prevent student access
2. **Question Protection**: Cannot delete questions from tests with completed attempts (data integrity)
3. **Subscription Limits**: Free users limited to 30 questions and 10 weekly invites
4. **Attempt Tracking**: Each student gets unique tokens and tracked attempts

### Frontend Architecture
- **Traditional MVC** with Razor views enhanced by **Bootstrap 5**
- **AJAX-heavy interactions** for dynamic content (question management, test editing)
- **Modal-based workflows** for creating/editing (tests, questions, invites)
- **Real-time UI updates** for status changes (test locking, filtering)

### Email System
Uses **SMTP configuration** in appsettings.json for sending test invitations. The `EmailService` abstraction allows for different email providers.

### Payment Integration
**Stripe** integration for subscription management:
- Checkout sessions for upgrades
- Webhook handling for subscription events
- Automatic subscription cleanup via background service

### Security Considerations
- **ASP.NET Core Identity** for authentication
- **Authorization attributes** on controller actions
- **CSRF protection** on state-changing operations
- **Data ownership validation** (users can only access their own tests)
- **Token-based test access** for students (unique per invitation)

### Configuration Notes
- **PostgreSQL** database (not SQLite in production)
- **User Secrets** for development configuration
- **Environment-specific** appsettings files
- **Stripe test keys** included in appsettings (should be moved to secrets for production)