using Application.Features.Clients.Commands.AddEdit;
using Application.Features.Produits.Queries.PaginationQuery;
using Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Indotalent.Pages
{
    public class ProduitsModel : PageModel
    {
        private readonly ISender _mediator;

        public ProduitsModel(ISender mediator)
        {
            _mediator = mediator;
        }
        public void OnGet()
        {
            this.SetupViewDataTitleFromUrl();
        }
        public async Task<IActionResult> OnGetDataAsync([FromQuery] ProduitsWithPaginationQuery command)
        {
            var result = await _mediator.Send(command);
            return new JsonResult(result);
        }
    }
}
