using Application.Features.Clients.Commands.AddEdit;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Indotalent.Pages
{
    public class IndexModel : PageModel
    {
        [BindProperty]
        public AddEditClientCommand Input { get; set; }
        private readonly ISender _mediator;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(ILogger<IndexModel> logger, ISender mediator)
        {
            _logger = logger;
            _mediator = mediator;
        }

        public void OnGet()
        {
            this.SetupViewDataTitleFromUrl();

        }

        public async Task<IActionResult> OnPostAsync()
        {
            var result = await _mediator.Send(Input);
            return new JsonResult(result);
        }
    }
}
