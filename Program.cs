using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TestPlatform2.Data;
using TestPlatform2.Repository;
using TestPlatform2.Services;

namespace TestPlatform2;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        
        // Register ApplicationDbContext with Identity
        builder.Services.AddDbContext<ApplicationDbContext>(options => 
            options.UseNpgsql(connectionString));
        builder.Services.AddDatabaseDeveloperPageExceptionFilter();

        // Configure Identity with custom User and Role (using string keys)
        builder.Services.AddIdentity<User, IdentityRole>(options => 
        {
            options.Password.RequireDigit = false;
            options.Password.RequiredLength = 6;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireUppercase = false;
            
            // Email verification settings
            options.SignIn.RequireConfirmedEmail = true;
            options.User.RequireUniqueEmail = true;
        })
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();

        builder.Services.AddControllersWithViews();

        // Repository services
        builder.Services.AddScoped<ITestRepository, TestRepository>();
        builder.Services.AddScoped<IQuestionRepository, QuestionRepository>();
        builder.Services.AddTransient<IEmailService, SmtpEmailService>();
        builder.Services.AddScoped<IAnswerRepository, AnswerRepository>();
        builder.Services.AddScoped<ITestAttemptRepository, TestAttemptRepository>();
        builder.Services.AddScoped<ITestInviteRepository, TestInviteRepository>();
        builder.Services.AddScoped<ITestAnalyticsRepository, TestAnalyticsRepository>();
        builder.Services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
        builder.Services.AddScoped<IExportService, ExportService>();
        builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
        builder.Services.AddScoped<ITagRepository, TagRepository>();
        builder.Services.AddScoped<IImageService, ImageService>();
        
        // Background services
        builder.Services.AddHostedService<SubscriptionCleanupService>();
        builder.Services.AddHostedService<TestSchedulingService>();

        // Add HttpContextAccessor for accessing HttpContext in services
        builder.Services.AddHttpContextAccessor();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseMigrationsEndPoint(); // Provides detailed DB errors in development
        }
        else
        {
            app.UseExceptionHandler("/Home/Error");
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseStaticFiles();
        
        app.UseRouting();

        // Authentication & Authorization Middleware
        app.UseAuthentication(); // Must come before UseAuthorization
        app.UseAuthorization();

        // Map Controllers and Razor Pages (if needed)
        app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}");
        
        // Only map Razor Pages if you're using them (e.g., Identity UI)
        // app.MapRazorPages(); 

        app.Urls.Add("http://0.0.0.0:5000");

        using (var scope = app.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            dbContext.Database.Migrate();
        }
        app.Run();
    }
}