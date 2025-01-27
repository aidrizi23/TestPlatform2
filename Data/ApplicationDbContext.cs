using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TestPlatform2.Data.Questions;

namespace TestPlatform2.Data
{
    // Inherit from IdentityDbContext<User> instead of IdentityDbContext
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
            // Configure TPH inheritance
            modelBuilder.Entity<Question>()
                .HasDiscriminator<QuestionType>("QuestionType")
                .HasValue<MultipleChoiceQuestion>(QuestionType.MultipleChoice)
                .HasValue<TrueFalseQuestion>(QuestionType.TrueFalse)
                .HasValue<ShortAnswerQuestion>(QuestionType.ShortAnswer);

            // Configure owned entities for complex properties
            modelBuilder.Entity<MultipleChoiceQuestion>(mcq =>
            {
                mcq.OwnsMany(m => m.Options, o =>
                {
                    o.WithOwner().HasForeignKey("QuestionId");
                    o.Property<string>("Id");
                    o.HasKey("Id");
                });
            });
            
        }

        public enum QuestionType
        {
            MultipleChoice,
            TrueFalse,
            ShortAnswer
        }
    }
}