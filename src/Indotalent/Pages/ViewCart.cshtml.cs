using Application.Common.Extensions;
using Application.Common.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;

namespace Indotalent.Pages
{
    public class ViewCartModel : PageModel
    {
        public List<CartItem> CartItems { get; set; } = new();

        [BindProperty]
        public List<int> Quantities { get; set; }

        public void OnGet()
        {
            CartItems = GetCartFromSession();
        }

        public IActionResult OnPost(string action, int? removeIndex)
        {
            CartItems = GetCartFromSession();

            if (action == "update" && Quantities != null)
            {
                for (int i = 0; i < CartItems.Count; i++)
                {
                    CartItems[i].Quantity = Quantities[i];
                }
            }
            else if (removeIndex.HasValue)
            {
                CartItems.RemoveAt(removeIndex.Value);
            }

            SaveCartToSession(CartItems);
            return RedirectToPage();
        }

        private List<CartItem> GetCartFromSession()
        {
            return HttpContext.Session.GetObject<List<CartItem>>("Cart") ?? new List<CartItem>();
        }

        private void SaveCartToSession(List<CartItem> cart)
        {
            HttpContext.Session.SetObject("Cart", cart);
        }
    }
}
