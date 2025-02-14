using Application.Common.Extensions;
using Application.Common.Models;
using Application.Features.Produits.Queries.PaginationQuery;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;

namespace Indotalent.Pages
{
    public class ProduitsModel : PageModel
    {
        private readonly ISender _mediator;
        public List<CartItem> Cart { get; set; } = new();
        public ProduitsModel(ISender mediator)
        {
            _mediator = mediator;
        }
        public void OnGet()
        {
            Cart = HttpContext.Session.GetObject<List<CartItem>>("Cart") ?? new List<CartItem>();
            this.SetupViewDataTitleFromUrl();
        }
        public async Task<IActionResult> OnGetDataAsync([FromQuery] ProduitsWithPaginationQuery command)
        {
            var result = await _mediator.Send(command);
            return new JsonResult(result);
        }
    }
}
