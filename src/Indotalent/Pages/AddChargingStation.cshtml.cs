using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Indotalent.Pages
{
    public class AddChargingStationModel : PageModel
    {
        [BindProperty]
        public string ChargerName { get; set; }

        [BindProperty]
        public string SerialNumber { get; set; }

        [BindProperty]
        public string Puk { get; set; }

        public void OnGet()
        {
            // Initialize if needed
        }

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Save or pass along the data...
            // For now, just redirect somewhere
            return RedirectToPage("NetworkDetails");
        }
    }
}
