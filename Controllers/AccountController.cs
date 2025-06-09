using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using TestPlatform2.Data;
using TestPlatform2.Models.Account;
using TestPlatform2.Services;
using TestPlatform2.Helpers;

namespace TestPlatform2.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly IEmailService _emailService;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            IEmailService emailService,
            ILogger<AccountController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailService = emailService;
            _logger = logger;
        }

        // GET: /Account/Register
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        // POST: /Account/Register
        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new User
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FirstName = model.FirstName,
                    LastName = model.LastName
                };

                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    try
                    {
                        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                        var confirmationUrl = UrlHelper.BuildAbsoluteUrl(Request, 
                            Url.Action("ConfirmEmail", "Account", new { userId = user.Id, token }));

                        var emailBody = $@"
                            <html>
                            <head>
                                <style>
                                    body {{ font-family: Arial, sans-serif; margin: 0; padding: 20px; background-color: #f5f5f5; }}
                                    .container {{ max-width: 600px; margin: 0 auto; background-color: white; padding: 30px; border-radius: 8px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }}
                                    .header {{ text-align: center; margin-bottom: 30px; }}
                                    .header h1 {{ color: #0d6efd; margin: 0; }}
                                    .content {{ line-height: 1.6; color: #333; }}
                                    .btn {{ display: inline-block; background-color: #0d6efd; color: white; padding: 12px 24px; text-decoration: none; border-radius: 4px; margin: 20px 0; }}
                                    .footer {{ margin-top: 30px; padding-top: 20px; border-top: 1px solid #eee; font-size: 12px; color: #666; text-align: center; }}
                                </style>
                            </head>
                            <body>
                                <div class='container'>
                                    <div class='header'>
                                        <h1>Welcome to TestPlatform!</h1>
                                    </div>
                                    <div class='content'>
                                        <p>Hello {user.FirstName},</p>
                                        <p>Thank you for registering with TestPlatform. To complete your account setup, please confirm your email address by clicking the button below:</p>
                                        <p style='text-align: center;'>
                                            <a href='{confirmationUrl}' class='btn'>Confirm Email Address</a>
                                        </p>
                                        <p>If the button doesn't work, you can copy and paste this link into your browser:</p>
                                        <p style='word-break: break-all; color: #0d6efd;'>{confirmationUrl}</p>
                                        <p>This link will expire in 24 hours for security reasons.</p>
                                        <p>If you didn't create this account, please ignore this email.</p>
                                    </div>
                                    <div class='footer'>
                                        <p>This email was sent from TestPlatform. Please do not reply to this email.</p>
                                    </div>
                                </div>
                            </body>
                            </html>";

                        await _emailService.SendEmailAsync(user.Email, "Confirm your email address - TestPlatform", emailBody);
                        
                        _logger.LogInformation("Email confirmation sent to {Email}", user.Email);
                        
                        TempData["SuccessMessage"] = $"Registration successful! We've sent a confirmation email to {user.Email}. Please check your inbox and click the confirmation link to activate your account.";
                        return RedirectToAction("RegisterConfirmation");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send confirmation email to {Email}", user.Email);
                        
                        // Still allow the user to know they registered successfully, but email failed
                        TempData["WarningMessage"] = "Account created successfully, but we couldn't send the confirmation email. Please contact support for assistance.";
                        return RedirectToAction("RegisterConfirmation");
                    }
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            return View(model);
        }

        // GET: /Account/Login
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var result = await _signInManager.PasswordSignInAsync(
                    model.Email, model.Password, model.RememberMe, lockoutOnFailure: false
                );

                if (result.Succeeded)
                {
                    _logger.LogInformation("User {Email} logged in successfully", model.Email);
                    return RedirectToAction("Index", "Home");
                }

                if (result.IsNotAllowed)
                {
                    var user = await _userManager.FindByEmailAsync(model.Email);
                    if (user != null && !user.EmailConfirmed)
                    {
                        _logger.LogWarning("User {Email} attempted to login without confirming email", model.Email);
                        TempData["ErrorMessage"] = "You must confirm your email before you can log in.";
                        TempData["ResendEmail"] = model.Email;
                        return RedirectToAction("ResendConfirmation");
                    }
                    
                    ModelState.AddModelError(string.Empty, "Your account is not allowed to sign in.");
                }
                else if (result.IsLockedOut)
                {
                    _logger.LogWarning("User {Email} account is locked out", model.Email);
                    ModelState.AddModelError(string.Empty, "Your account is locked out.");
                }
                else
                {
                    _logger.LogWarning("Invalid login attempt for {Email}", model.Email);
                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                }
            }
            return View(model);
        }

        // POST: /Account/Logout
        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        // GET: /Account/RegisterConfirmation
        [HttpGet]
        public IActionResult RegisterConfirmation()
        {
            return View();
        }

        // GET: /Account/ConfirmEmail
        [HttpGet]
        public async Task<IActionResult> ConfirmEmail(string userId, string token)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token))
            {
                TempData["ErrorMessage"] = "Invalid email confirmation link.";
                return RedirectToAction("Login");
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction("Login");
            }

            if (user.EmailConfirmed)
            {
                TempData["InfoMessage"] = "Your email is already confirmed. You can log in.";
                return RedirectToAction("Login");
            }

            var result = await _userManager.ConfirmEmailAsync(user, token);
            if (result.Succeeded)
            {
                _logger.LogInformation("Email confirmed successfully for user {Email}", user.Email);
                TempData["SuccessMessage"] = "Thank you for confirming your email. You can now log in to your account.";
                return RedirectToAction("Login");
            }

            _logger.LogWarning("Email confirmation failed for user {Email}", user.Email);
            TempData["ErrorMessage"] = "Error confirming your email. The link may have expired or is invalid.";
            return RedirectToAction("Login");
        }

        // GET: /Account/ResendConfirmation
        [HttpGet]
        public IActionResult ResendConfirmation()
        {
            return View();
        }

        // POST: /Account/ResendConfirmation
        [HttpPost]
        public async Task<IActionResult> ResendConfirmation(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                ModelState.AddModelError(string.Empty, "Email address is required.");
                return View();
            }

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                // Don't reveal that the user doesn't exist
                TempData["SuccessMessage"] = "If an account with that email exists, we've sent a confirmation email.";
                return RedirectToAction("RegisterConfirmation");
            }

            if (user.EmailConfirmed)
            {
                TempData["InfoMessage"] = "Your email is already confirmed. You can log in.";
                return RedirectToAction("Login");
            }

            try
            {
                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                var confirmationUrl = UrlHelper.BuildAbsoluteUrl(Request,
                    Url.Action("ConfirmEmail", "Account", new { userId = user.Id, token }));

                var emailBody = $@"
                    <html>
                    <head>
                        <style>
                            body {{ font-family: Arial, sans-serif; margin: 0; padding: 20px; background-color: #f5f5f5; }}
                            .container {{ max-width: 600px; margin: 0 auto; background-color: white; padding: 30px; border-radius: 8px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }}
                            .header {{ text-align: center; margin-bottom: 30px; }}
                            .header h1 {{ color: #0d6efd; margin: 0; }}
                            .content {{ line-height: 1.6; color: #333; }}
                            .btn {{ display: inline-block; background-color: #0d6efd; color: white; padding: 12px 24px; text-decoration: none; border-radius: 4px; margin: 20px 0; }}
                            .footer {{ margin-top: 30px; padding-top: 20px; border-top: 1px solid #eee; font-size: 12px; color: #666; text-align: center; }}
                        </style>
                    </head>
                    <body>
                        <div class='container'>
                            <div class='header'>
                                <h1>Email Confirmation - TestPlatform</h1>
                            </div>
                            <div class='content'>
                                <p>Hello {user.FirstName},</p>
                                <p>We received a request to resend your email confirmation. Please confirm your email address by clicking the button below:</p>
                                <p style='text-align: center;'>
                                    <a href='{confirmationUrl}' class='btn'>Confirm Email Address</a>
                                </p>
                                <p>If the button doesn't work, you can copy and paste this link into your browser:</p>
                                <p style='word-break: break-all; color: #0d6efd;'>{confirmationUrl}</p>
                                <p>This link will expire in 24 hours for security reasons.</p>
                                <p>If you didn't request this, please ignore this email.</p>
                            </div>
                            <div class='footer'>
                                <p>This email was sent from TestPlatform. Please do not reply to this email.</p>
                            </div>
                        </div>
                    </body>
                    </html>";

                await _emailService.SendEmailAsync(user.Email, "Confirm your email address - TestPlatform", emailBody);
                
                _logger.LogInformation("Confirmation email resent to {Email}", user.Email);
                TempData["SuccessMessage"] = "Confirmation email sent. Please check your inbox.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to resend confirmation email to {Email}", email);
                TempData["ErrorMessage"] = "Failed to send confirmation email. Please try again later.";
            }

            return RedirectToAction("RegisterConfirmation");
        }

        // GET: /Account/ForgotPassword
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        // POST: /Account/ForgotPassword
        [HttpPost]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                ModelState.AddModelError(string.Empty, "Email address is required.");
                return View();
            }

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null || !user.EmailConfirmed)
            {
                // Don't reveal that the user doesn't exist or isn't confirmed
                TempData["SuccessMessage"] = "If an account with that email exists, we've sent a password reset link.";
                return RedirectToAction("ForgotPasswordConfirmation");
            }

            try
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var resetUrl = UrlHelper.BuildAbsoluteUrl(Request,
                    Url.Action("ResetPassword", "Account", new { userId = user.Id, token }));

                var emailBody = $@"
                    <html>
                    <head>
                        <style>
                            body {{ font-family: Arial, sans-serif; margin: 0; padding: 20px; background-color: #f5f5f5; }}
                            .container {{ max-width: 600px; margin: 0 auto; background-color: white; padding: 30px; border-radius: 8px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }}
                            .header {{ text-align: center; margin-bottom: 30px; }}
                            .header h1 {{ color: #dc3545; margin: 0; }}
                            .content {{ line-height: 1.6; color: #333; }}
                            .btn {{ display: inline-block; background-color: #dc3545; color: white; padding: 12px 24px; text-decoration: none; border-radius: 4px; margin: 20px 0; }}
                            .footer {{ margin-top: 30px; padding-top: 20px; border-top: 1px solid #eee; font-size: 12px; color: #666; text-align: center; }}
                            .security-note {{ background-color: #fff3cd; border: 1px solid #ffeaa7; border-radius: 4px; padding: 15px; margin: 20px 0; color: #856404; }}
                        </style>
                    </head>
                    <body>
                        <div class='container'>
                            <div class='header'>
                                <h1>Reset Your Password - TestPlatform</h1>
                            </div>
                            <div class='content'>
                                <p>Hello {user.FirstName},</p>
                                <p>We received a request to reset your password for your TestPlatform account. If you made this request, click the button below to reset your password:</p>
                                <p style='text-align: center;'>
                                    <a href='{resetUrl}' class='btn'>Reset Password</a>
                                </p>
                                <p>If the button doesn't work, you can copy and paste this link into your browser:</p>
                                <p style='word-break: break-all; color: #dc3545;'>{resetUrl}</p>
                                
                                <div class='security-note'>
                                    <strong>Security Note:</strong>
                                    <ul style='margin: 5px 0;'>
                                        <li>This link will expire in 1 hour for security reasons</li>
                                        <li>If you didn't request this password reset, please ignore this email</li>
                                        <li>Your password will not be changed until you click the link and create a new one</li>
                                    </ul>
                                </div>
                                
                                <p>If you continue to have problems accessing your account, please contact our support team.</p>
                            </div>
                            <div class='footer'>
                                <p>This email was sent from TestPlatform. Please do not reply to this email.</p>
                                <p>If you didn't request this password reset, your account is still secure. You can safely ignore this email.</p>
                            </div>
                        </div>
                    </body>
                    </html>";

                await _emailService.SendEmailAsync(user.Email, "Reset your password - TestPlatform", emailBody);
                
                _logger.LogInformation("Password reset email sent to {Email}", user.Email);
                TempData["SuccessMessage"] = "Password reset link sent. Please check your email.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send password reset email to {Email}", email);
                TempData["ErrorMessage"] = "Failed to send password reset email. Please try again later.";
            }

            return RedirectToAction("ForgotPasswordConfirmation");
        }

        // GET: /Account/ForgotPasswordConfirmation
        [HttpGet]
        public IActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

        // GET: /Account/ResetPassword
        [HttpGet]
        public IActionResult ResetPassword(string userId, string token)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token))
            {
                TempData["ErrorMessage"] = "Invalid password reset link.";
                return RedirectToAction("Login");
            }

            var model = new ResetPasswordViewModel
            {
                Token = token,
                UserId = userId
            };

            return View(model);
        }

        // POST: /Account/ResetPassword
        [HttpPost]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction("Login");
            }

            var result = await _userManager.ResetPasswordAsync(user, model.Token, model.Password);
            if (result.Succeeded)
            {
                _logger.LogInformation("Password reset successfully for user {Email}", user.Email);
                TempData["SuccessMessage"] = "Your password has been reset successfully. You can now log in with your new password.";
                return RedirectToAction("Login");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            _logger.LogWarning("Password reset failed for user {Email}. Errors: {Errors}", 
                user.Email, string.Join(", ", result.Errors.Select(e => e.Description)));

            return View(model);
        }
    }
}