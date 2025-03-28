using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Infrastructure;
using Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Indotalent.Pages
{
    public class AllChargersModel : PageModel
    {
        private readonly ApplicationDbContext _dbContext;

        public AllChargersModel(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public List<ChargingStation> Chargers { get; set; }

        public async Task OnGetAsync()
        {
            // Eagerly load related network data
            Chargers = await _dbContext.ChargingStations
                .Include(cs => cs.Network)
                .ToListAsync();
        }
    }
}
