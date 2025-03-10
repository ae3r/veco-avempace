using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Indotalent.Pages
{
    public class NetworkListModel : PageModel
    {
        public string NetworkName { get; set; }
        public string MeterPower { get; set; } // store the string version

        public void OnGet()
        {
            // Attempt to retrieve from TempData
            // (If empty, default to something)

            NetworkName = TempData["NetworkName"] as string ?? "Test";

            // MeterPower is also stored as string
            MeterPower = TempData["MeterPower"] as string ?? "0";

            // If you want to parse to double, you could do:
            // double.TryParse(MeterPower, out double mpValue);
            // But for display, the string is fine.
        }
    }
}
