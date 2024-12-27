using Application.Features.Clients.Commands.AddEdit;
using Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Indotalent.Pages
{
    public class ProduitsModel : PageModel
    {
        [BindProperty]
        public AddEditClientCommand Input { get; set; }
        private readonly ISender _mediator;

        public ProduitsModel(ISender mediator)
        {
            _mediator = mediator;
        }
        public void OnGet()
        {
            this.SetupViewDataTitleFromUrl();
        }
        public async Task<IActionResult> OnPostAsync([FromQuery] int type, [FromQuery] string distance, [FromQuery] string isConsumptionMonitoring)
        {
            Input.TypeWF = (TypeWFEnum)type;
            Input.Distance = distance;
            Input.IsConsumptionMonitoring = isConsumptionMonitoring;
            var result = await _mediator.Send(Input);
            return new JsonResult(result);
        }
    }
}
