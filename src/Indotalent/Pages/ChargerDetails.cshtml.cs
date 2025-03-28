using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Infrastructure;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace Indotalent.Pages
{
    public class ChargerDetailsModel : PageModel
    {
        private readonly ApplicationDbContext _dbContext;

        public ChargerDetailsModel(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        // The charger id is provided in the URL (e.g., /ChargerDetails/123)
        [BindProperty(SupportsGet = true)]
        public int ChargerId { get; set; }

        public ChargingStation? Charger { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            Charger = await _dbContext.ChargingStations
                        .Include(cs => cs.Network) // include related network if needed
                        .FirstOrDefaultAsync(cs => cs.Id == ChargerId);

            if (Charger == null)
            {
                return NotFound();
            }
            return Page();
        }
    }
}
