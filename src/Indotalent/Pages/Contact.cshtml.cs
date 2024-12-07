using Application.Features.Clients.Commands.AddEdit;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Indotalent.Pages
{
    public class ContactModel : PageModel
    {
        [BindProperty]
        public AddEditClientCommand Input { get; set; }
        private readonly ISender _mediator;

        public ContactModel(ISender mediator)
        {
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
