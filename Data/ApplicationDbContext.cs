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
        
        public DbSet<Subscription> Subscriptions { get; set; }
        public DbSet<SubscriptionHistory> SubscriptionHistories { get; set; }
        
        public DbSet<TestCategory> TestCategories { get; set; }
        public DbSet<TestTag> TestTags { get; set; }
      

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
                .HasValue<ShortAnswerQuestion>(ApplicationDbContext.QuestionType.ShortAnswer)
                .HasValue<DragDropQuestion>(ApplicationDbContext.QuestionType.DragDrop)
                .HasValue<TableQuestion>(ApplicationDbContext.QuestionType.Table);

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

            // TestCategory relationships
            modelBuilder.Entity<TestCategory>()
                .HasOne(c => c.User)
                .WithMany()
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Test>()
                .HasOne(t => t.Category)
                .WithMany(c => c.Tests)
                .HasForeignKey(t => t.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);

            // TestTag relationships
            modelBuilder.Entity<TestTag>()
                .HasOne(tag => tag.User)
                .WithMany()
                .HasForeignKey(tag => tag.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Many-to-many relationship between Test and TestTag
            modelBuilder.Entity<Test>()
                .HasMany(t => t.Tags)
                .WithMany(tag => tag.Tests)
                .UsingEntity<Dictionary<string, object>>(
                    "TestTagRelation",
                    j => j.HasOne<TestTag>().WithMany().HasForeignKey("TagId"),
                    j => j.HasOne<Test>().WithMany().HasForeignKey("TestId"),
                    j =>
                    {
                        j.HasKey("TestId", "TagId");
                        j.ToTable("TestTagRelations");
                    });

        }

        public enum QuestionType
        {
            MultipleChoice,
            TrueFalse,
            ShortAnswer,
            DragDrop,
            Table
        }
    }
}