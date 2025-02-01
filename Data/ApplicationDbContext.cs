// ApplicationDbContext.cs
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TestPlatform2.Data.Questions;

namespace TestPlatform2.Data
{
    public class ApplicationDbContext : IdentityDbContext<User>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Test> Tests { get; set; }
        public DbSet<Question> Questions { get; set; }
        
        public DbSet<TestInvite> TestInvites { get; set; }
        public DbSet<TestAttempt> TestAttempts { get; set; }
        public DbSet<Answer> Answers { get; set; }
        
        
        
        // protected override void OnModelCreating(ModelBuilder modelBuilder)
        // {
        //     base.OnModelCreating(modelBuilder);
        //
        //     // Configuring Test and User relationship
        //     modelBuilder.Entity<Test>()
        //         .HasOne(t => t.User)
        //         .WithMany(u => u.Tests)
        //         .HasForeignKey(t => t.UserId)
        //         .OnDelete(DeleteBehavior.Restrict); // Prevent cascading delete
        //
        //     // Configuring Question hierarchy
        //     modelBuilder.Entity<Question>()
        //         .HasDiscriminator<ApplicationDbContext.QuestionType>("QuestionType")
        //         .HasValue<MultipleChoiceQuestion>(ApplicationDbContext.QuestionType.MultipleChoice)
        //         .HasValue<TrueFalseQuestion>(ApplicationDbContext.QuestionType.TrueFalse)
        //         .HasValue<ShortAnswerQuestion>(ApplicationDbContext.QuestionType.ShortAnswer);
        //     
        //     // TestInvite relationships
        //     modelBuilder.Entity<TestInvite>()
        //         .HasOne(i => i.Test)
        //         .WithMany(t => t.InvitedStudents)
        //         .HasForeignKey(i => i.TestId);
        //
        //     // TestAttempt relationships
        //     modelBuilder.Entity<TestAttempt>()
        //         .HasOne(a => a.Test)
        //         .WithMany(t => t.Attempts)
        //         .HasForeignKey(a => a.TestId);
        //
        //     // Answer relationships
        //     modelBuilder.Entity<Answer>()
        //         .HasOne(a => a.Question)
        //         .WithMany()
        //         .HasForeignKey(a => a.QuestionId);
        //
        //     modelBuilder.Entity<Answer>()
        //         .HasOne(a => a.Attempt)
        //         .WithMany(a => a.Answers)
        //         .HasForeignKey(a => a.AttemptId);
        // }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configuring Test and User relationship
            modelBuilder.Entity<Test>()
                .HasOne(t => t.User)
                .WithMany(u => u.Tests)
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Restrict); // Prevent cascading delete

            // Configuring Question hierarchy
            modelBuilder.Entity<Question>()
                .HasDiscriminator<ApplicationDbContext.QuestionType>("QuestionType")
                .HasValue<MultipleChoiceQuestion>(ApplicationDbContext.QuestionType.MultipleChoice)
                .HasValue<TrueFalseQuestion>(ApplicationDbContext.QuestionType.TrueFalse)
                .HasValue<ShortAnswerQuestion>(ApplicationDbContext.QuestionType.ShortAnswer);

            // TestInvite relationships
            modelBuilder.Entity<TestInvite>()
                .HasOne(i => i.Test)
                .WithMany(t => t.InvitedStudents)
                .HasForeignKey(i => i.TestId);

            // TestAttempt relationships
            modelBuilder.Entity<TestAttempt>()
                .HasOne(a => a.Test)
                .WithMany(t => t.Attempts)
                .HasForeignKey(a => a.TestId);

            // Answer relationships
            modelBuilder.Entity<Answer>()
                .HasOne(a => a.Question)
                .WithMany()
                .HasForeignKey(a => a.QuestionId)
                .OnDelete(DeleteBehavior.Cascade); // Allow cascading delete for questions

            modelBuilder.Entity<Answer>()
                .HasOne(a => a.Attempt)
                .WithMany(a => a.Answers)
                .HasForeignKey(a => a.AttemptId)
                .OnDelete(DeleteBehavior.Restrict); // Restrict cascading delete for attempts
        }

        public enum QuestionType
        {
            MultipleChoice,
            TrueFalse,
            ShortAnswer
        }
    }
}