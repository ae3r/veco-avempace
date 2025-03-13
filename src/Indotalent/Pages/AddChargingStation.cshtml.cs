using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;

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

        // We'll store a numeric input in "PowerValue", then append " kW".
        [BindProperty]
        public string PowerValue { get; set; }

        public void OnGet()
        {
        }

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            // If user typed "40", we store "40 kW".
            // If blank, we store "0 kW".
            string finalPower = "0 kW";
            if (!string.IsNullOrWhiteSpace(PowerValue))
            {
                finalPower = PowerValue.Trim() + " kW";
            }

            // Add the new station to the in-memory store
            ChargingStationStore.Stations.Add(new ChargerStationData
            {
                ChargerName = ChargerName,
                SerialNumber = SerialNumber,
                Puk = Puk,
                Status = "In charging", // or default
                PowerKW = finalPower,
                CreatedAt = DateTime.Now
            });

            return RedirectToPage("NetworkDetails");
        }
    }
}
