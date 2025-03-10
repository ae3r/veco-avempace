using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Indotalent.Pages
{
    public class SystemParametersModel : PageModel
    {
        [BindProperty]
        public string TypeOfMeter { get; set; }

        // We'll store meter power as a string property so we can parse it
        [BindProperty]
        public string MeterPower { get; set; }

        public void OnGet()
        {
            // (Optional) retrieve data from TempData from Step 1, e.g. the network name
            // var networkName = TempData["NetworkName"] as string;
            // TempData.Keep(); // if you need to keep it
        }

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Store the chosen meter type
            TempData["TypeOfMeter"] = TypeOfMeter;

            // Store meter power as a string
            TempData["MeterPower"] = MeterPower;

            // Redirect to NetworkList, which will read these TempData values
            return RedirectToPage("NetworkList");
        }
    }
}
