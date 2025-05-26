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
        
        // Subscription related
        public DbSet<Subscription> Subscriptions { get; set; }
        public DbSet<SubscriptionPlan> SubscriptionPlans { get; set; }

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
                
            // Subscription relationships
            modelBuilder.Entity<Subscription>()
                .HasOne(s => s.User)
                .WithOne(u => u.Subscription)
                .HasForeignKey<Subscription>(s => s.UserId)
                .OnDelete(DeleteBehavior.Restrict);
                
            // Subscription indexes
            modelBuilder.Entity<Subscription>()
                .HasIndex(s => s.StripeCustomerId)
                .IsUnique();
                
            modelBuilder.Entity<Subscription>()
                .HasIndex(s => s.StripeSubscriptionId)
                .IsUnique();
                
            modelBuilder.Entity<User>()
                .HasIndex(u => u.StripeCustomerId)
                .IsUnique()
                .HasFilter("[StripeCustomerId] IS NOT NULL");
                
            // Seed subscription plans
            modelBuilder.Entity<SubscriptionPlan>().HasData(
                new SubscriptionPlan
                {
                    Id = "free-plan",
                    Name = "Free",
                    Description = "Basic features for getting started",
                    Tier = SubscriptionTier.Free,
                    Price = 0,
                    Currency = "usd",
                    StripePriceId = null,
                    MaxQuestionsPerTest = 30,
                    MaxTestsPerMonth = 5,
                    MaxStudentsPerTest = 50,
                    AdvancedAnalytics = false,
                    PrioritySupport = false
                },
                new SubscriptionPlan
                {
                    Id = "pro-plan",
                    Name = "Pro",
                    Description = "Unlimited questions and advanced features",
                    Tier = SubscriptionTier.Pro,
                    Price = 5.00m,
                    Currency = "usd",
                    StripePriceId = "price_xxxxx", // You'll need to update this with your actual Stripe price ID
                    MaxQuestionsPerTest = -1, // -1 means unlimited
                    MaxTestsPerMonth = -1,
                    MaxStudentsPerTest = -1,
                    AdvancedAnalytics = true,
                    PrioritySupport = true
                }
            );
        }

        public enum QuestionType
        {
            MultipleChoice,
            TrueFalse,
            ShortAnswer
        }
    }
}