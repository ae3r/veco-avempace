using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Infrastructure;
using Domain.Entities;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Indotalent.Pages
{
    public class CreateChargerModel : PageModel
    {
        private readonly ApplicationDbContext _dbContext;

        public CreateChargerModel(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [BindProperty, Required(ErrorMessage = "Charger Name is required.")]
        public string ChargerName { get; set; }

        [BindProperty, Required(ErrorMessage = "Serial Number is required.")]
        public string SerialNumber { get; set; }

        [BindProperty, Required(ErrorMessage = "PUK is required.")]
        public string Puk { get; set; }

        [BindProperty]
        public double? PowerValue { get; set; }

        [BindProperty]
        public string ChargerStatus { get; set; } = "New";

        [BindProperty]
        public string ConnectionStatus { get; set; } = "Disconnected";

        [BindProperty]
        public string Model { get; set; } = "Default Model";

        // Use [FromQuery] to ensure the parameter comes from the URL query string.
        [FromQuery]
        public int NetworkId { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            // Debug: Output the query string
            System.Diagnostics.Debug.WriteLine("Request.QueryString: " + Request.QueryString);
            // Check if network exists
            var networkExists = await _dbContext.Networks.AnyAsync(n => n.Id == NetworkId);
            if (!networkExists)
            {
                return NotFound("The specified network does not exist.");
            }
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var networkExists = await _dbContext.Networks.AnyAsync(n => n.Id == NetworkId);
            if (!networkExists)
            {
                ModelState.AddModelError("NetworkId", "The selected network does not exist.");
                return Page();
            }

            double power = PowerValue ?? 0;

            var newCharger = new ChargingStation
            {
                ChargerName = ChargerName,
                SerialNumber = SerialNumber,
                Puk = Puk,
                PowerValue = power,
                ChargerStatus = ChargerStatus,
                ConnectionStatus = ConnectionStatus,
                Model = Model,
                NetworkId = NetworkId
            };

            _dbContext.ChargingStations.Add(newCharger);
            await _dbContext.SaveChangesAsync();

            return RedirectToPage("AllChargers");
        }
    }
}
