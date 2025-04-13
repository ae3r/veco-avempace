using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Domain.Entities; // your Network entity namespace
using Infrastructure;  // for ApplicationDbContext
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

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

        [BindProperty]
        public string Street { get; set; }

        [BindProperty]
        public string City { get; set; }

        [BindProperty]
        public string ZipCode { get; set; }

        [BindProperty]
        public string Country { get; set; }

        // NEW PROPERTIES
        [BindProperty]
        public string MeterType { get; set; }   // "Single phase" or "Three phase"

        [BindProperty]
        [RegularExpression(@"^\d+$", ErrorMessage = "Meter power must be an integer.")]
        public string MeterValue { get; set; }

        // NEW: Photovoltaic toggle & facility type
        [BindProperty]
        public bool HasPhotovoltaic { get; set; }

        // REMOVED [Required], replaced with custom check in OnPostAsync()
        [BindProperty]
        public string? PhotovoltaicFacilityType { get; set; }  // "Single phase" or "Three phase"

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Regular model validation first (checks e.g. MeterValue must be numeric)
            if (!ModelState.IsValid)
                return Page();

            // CUSTOM VALIDATION: If user checks Photovoltaic, then they must pick a facility type
            if (HasPhotovoltaic && string.IsNullOrWhiteSpace(PhotovoltaicFacilityType))
            {
                ModelState.AddModelError(nameof(PhotovoltaicFacilityType),
                    "Type of facility is required if Photovoltaic is enabled.");
                return Page();
            }

            var newNetwork = new Network
            {
                NetworkName = NetworkName,
                Address = $"{Street}, {City}, {ZipCode}, {Country}",
                PhotoUrl = "default.png",
                Plant = "",
                NetworkLayout = "",
                NumberOfChargers = 0,
                NumberOfUsers = 0,
                Street = Street,
                City = City,
                ZipCode = ZipCode,
                Country = Country,

                MeterType = MeterType,
                MeterValue = MeterValue,

                HasPhotovoltaic = HasPhotovoltaic,
                // If not toggled, store null. If toggled, store whatever user selected
                PhotovoltaicFacilityType = HasPhotovoltaic
                     ? PhotovoltaicFacilityType
                     : null
            };

            _dbContext.Networks.Add(newNetwork);
            await _dbContext.SaveChangesAsync();

            return RedirectToPage("Networks");
        }
    }
}
