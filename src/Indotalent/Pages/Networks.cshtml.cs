using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization; // If you want only Admin
using Microsoft.EntityFrameworkCore;
using Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;
using Infrastructure; // your EF context namespace

namespace Indotalent.Pages
{
      // Only admin can view
    public class NetworksModel : PageModel
    {
        private readonly ApplicationDbContext _dbContext;

        public NetworksModel(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public List<Network> Networks { get; set; } = new List<Network>();

        // GET: Load the list of networks
        public async Task<IActionResult> OnGetAsync()
        {
            // Load from your DB. Adjust if needed for your table name
            Networks = await _dbContext.Networks.ToListAsync();
            return Page();
        }

        // POST: "Create Network" button
        public IActionResult OnPost()
        {
            // For now, just redirect to a hypothetical "CreateNetwork" page
            return RedirectToPage("CreateNetwork");
        }
    }
}
