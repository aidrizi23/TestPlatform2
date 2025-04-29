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
        })
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();

        builder.Services.AddControllersWithViews();

        builder.Services.AddScoped<ITestRepository, TestRepository>();
        builder.Services.AddScoped<IQuestionRepository, QuestionRepository>();
        builder.Services.AddTransient<IEmailService, SmtpEmailService>();
        builder.Services.AddScoped<IAnswerRepository, AnswerRepository>();
        builder.Services.AddScoped<ITestAttemptRepository, TestAttemptRepository>();
        builder.Services.AddScoped<ITestInviteRepository, TestInviteRepository>();
        
        builder.Services.AddScoped<ITestAnalyticsRepository, TestAnalyticsRepository>();

        

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

        app.Run();
    }
}