using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Infrastructure;        // or wherever your ApplicationDbContext is
using Domain.Entities;       // your Network entity
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Indotalent.Pages
{
    public class NetworkPanelModel : PageModel
    {
        private readonly ApplicationDbContext _dbContext;

        public NetworkPanelModel(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        // The network loaded from the database
        public Network CurrentNetwork { get; set; }

        // List of ChargingStations for the current network (used in the Rechargers tab)
        public List<ChargingStation> Chargers { get; set; } = new List<ChargingStation>();

        // Parameter for the network ID passed in (via query string)
        [BindProperty(SupportsGet = true)]
        public int NetworkId { get; set; }

        public async Task<IActionResult> OnGetAsync(int networkId)
        {
            NetworkId = networkId;
            CurrentNetwork = await _dbContext.Networks
                .Include(n => n.ChargingStations)
                .FirstOrDefaultAsync(n => n.Id == networkId);

            if (CurrentNetwork == null)
            {
                return NotFound("The specified network does not exist.");
            }

            Chargers = CurrentNetwork.ChargingStations.ToList();
            return Page();
        }

    }
}

