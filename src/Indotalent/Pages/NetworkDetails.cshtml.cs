using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Indotalent.Pages
{
    public class NetworkDetailsModel : PageModel
    {
        public string NetworkName { get; set; }

        public void OnGet(string networkName)
        {
            NetworkName = networkName ?? "Test";
        }
    }
}
