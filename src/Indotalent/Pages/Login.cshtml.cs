using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration; // For IConfiguration
using System;
using System.Data;
using System.Data.SqlClient;

namespace Indotalent.Pages
{
    public class LoginModel : PageModel
    {
        private readonly IConfiguration _config;

        public LoginModel(IConfiguration config)
        {
            _config = config;
        }

        [BindProperty]
        public string Email { get; set; }

        [BindProperty]
        public string Password { get; set; }

        public void OnGet()
        {
        }

        public IActionResult OnPost()
        {
            // 1) Check if the user is Admin
            if (IsAdminUser(Email, Password))
            {
                // 2) If valid, redirect to the "Networks" page
                return RedirectToPage("Networks");
            }
            else
            {
                // 3) Otherwise, remain on the same page or show error
                ModelState.AddModelError(string.Empty, "Invalid email or password.");
                return Page();
            }
        }

        private bool IsAdminUser(string email, string password)
        {
            bool isAdmin = false;

            // Grab the connection string from appsettings.json
            string connString = _config.GetConnectionString("DefaultConnection");

            // Basic ADO.NET approach:
            using (SqlConnection conn = new SqlConnection(connString))
            {
                conn.Open();

                // We’ll do a simple query. In production, param names should match the columns.
                string sql = @"
                    SELECT [IsAdmin]
                    FROM [dbo].[Users]
                    WHERE [Email] = @Email AND [Password] = @Password
                ";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.Add("@Email", SqlDbType.NVarChar).Value = email;
                    cmd.Parameters.Add("@Password", SqlDbType.NVarChar).Value = password;

                    var result = cmd.ExecuteScalar();
                    if (result != null)
                    {
                        // result should be True/False for the IsAdmin column
                        isAdmin = Convert.ToBoolean(result);
                    }
                }
            }

            return isAdmin;
        }

        // ---------- Existing partial handlers (if you still need them) ----------
        public IActionResult OnGetSignUpChoicePartial() => Partial("_SignUpChoicePartial");
        public IActionResult OnGetPrivatePurchaseSignupPartial() => Partial("_PrivatePurchaseSignupPartial");
        public IActionResult OnGetPrivatePurchaseSignupStep2Partial() => Partial("_PrivatePurchaseSignupStep2Partial");
    }
}
