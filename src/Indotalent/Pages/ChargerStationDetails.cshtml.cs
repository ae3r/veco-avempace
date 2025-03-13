using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Linq;

namespace Indotalent.Pages
{
    public class ChargerStationDetailsModel : PageModel
    {
        public ChargerStationData Station { get; set; }

        public void OnGet(string name)
        {
            Station = ChargingStationStore.Stations.FirstOrDefault(s => s.ChargerName == name);
        }
    }
}
