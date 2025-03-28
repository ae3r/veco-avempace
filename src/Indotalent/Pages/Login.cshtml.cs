using Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Threading.Tasks;

namespace Indotalent.Pages
{
    public class LoginModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;

        public LoginModel(SignInManager<ApplicationUser> signInManager)
        {
            _signInManager = signInManager;
        }

        [BindProperty]
        public string Email { get; set; }

        [BindProperty]
        public string Password { get; set; }

        public void OnGet()
        {
            // No special logic on GET
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // If no input or invalid, show same page with error
            if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
            {
                ModelState.AddModelError(string.Empty, "Email and Password are required.");
                return Page();
            }

            // Attempt sign-in
            var result = await _signInManager.PasswordSignInAsync(Email, Password,
                    isPersistent: false, lockoutOnFailure: false);

            if (result.Succeeded)
            {
                // If success, redirect to Networks
                return RedirectToPage("Networks");
            }
            else
            {
                // Show error
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                return Page();
            }
        }

        // If you still need the partial handlers for sign-up
        public IActionResult OnGetSignUpChoicePartial() => Partial("_SignUpChoicePartial");
        public IActionResult OnGetPrivatePurchaseSignupPartial() => Partial("_PrivatePurchaseSignupPartial");
        public IActionResult OnGetPrivatePurchaseSignupStep2Partial() => Partial("_PrivatePurchaseSignupStep2Partial");
    }
}
