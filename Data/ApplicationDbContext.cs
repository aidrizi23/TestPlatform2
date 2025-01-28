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
        }


        public enum QuestionType
        {
            MultipleChoice,
            TrueFalse,
            ShortAnswer
        }
    }
}