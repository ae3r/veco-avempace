using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Domain.Entities; // your Network entity namespace
using Infrastructure; // for ApplicationDbContext
using System.Threading.Tasks;

namespace Indotalent.Pages
{
    public class CreateNetworkModel : PageModel
    {
        private readonly ApplicationDbContext _dbContext;

        public CreateNetworkModel(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [BindProperty]
        public string NetworkName { get; set; }

        // New bindable properties for the address fields
        [BindProperty]
        public string Street { get; set; }

        [BindProperty]
        public string City { get; set; }

        [BindProperty]
        public string ZipCode { get; set; }

        [BindProperty]
        public string Country { get; set; }

        // Other properties if needed...

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            // Create the new Network object with the new fields.
            var newNetwork = new Network
            {
                NetworkName = NetworkName,
                // You can either combine Street with any other address info into Address,
                // or set Address separately if needed.
                Address = $"{Street}, {City}, {ZipCode}, {Country}",
                // Set default values for other required columns or collect them from the form.
                PhotoUrl = "default.png",   // Set a default image path
                Plant = "",                 // Default or empty if not provided
                NetworkLayout = "",         // Default value
                NumberOfChargers = 0,       // Default value
                NumberOfUsers = 0,          // Default value

                // Optionally, if you added separate columns for Street, City, etc. in your Network entity:
                Street = Street,
                City = City,
                ZipCode = ZipCode,
                Country = Country
            };

            _dbContext.Networks.Add(newNetwork);
            await _dbContext.SaveChangesAsync();

            return RedirectToPage("Networks");
        }
    }
}
