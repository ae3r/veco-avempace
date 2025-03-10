using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Indotalent.Pages
{
    public class CreateNetworkModel : PageModel
    {
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

        public void OnGet()
        {
        }

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Here you'd typically save the data or store it in TempData/Session
            // For the demo, let's just store it in TempData to pass to Step 2
            TempData["NetworkName"] = NetworkName;
            TempData["Street"] = Street;
            TempData["City"] = City;
            TempData["ZipCode"] = ZipCode;
            TempData["Country"] = Country;

            // Redirect to Step 2: SystemParameters
            return RedirectToPage("SystemParameters");
        }
    }
}
