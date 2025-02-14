using Application.Common.Extensions;
using Application.Common.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;

namespace Indotalent.Pages
{
    public class HeaderModel : PageModel
    {
        private readonly ISender _mediator;
        public List<CartItem> Cart { get; set; } = new();
        public HeaderModel(ISender mediator)
        {
            _mediator = mediator;
        }
        public void OnGet()
        {
            // Charger le panier depuis la session
            Cart = HttpContext.Session.GetObject<List<CartItem>>("Cart") ?? new List<CartItem>();
            //this.SetupViewDataTitleFromUrl();
        }

        public async Task<IActionResult> OnGetRefresh()
        {
            var cart = HttpContext.Session.GetObject<List<CartItem>>("Cart") ?? new List<CartItem>();
            return Partial("_HeaderPartial", cart); // Retourne la vue partielle mise à jour
        }

        public async Task<IActionResult> OnPostAddToCart()
        {
            // Lire le JSON envoyé par JavaScript
            var requestBody = await new System.IO.StreamReader(Request.Body).ReadToEndAsync();
            var item = JsonSerializer.Deserialize<CartItem>(requestBody);

            if (item == null) return BadRequest(new { success = false, message = "Invalid data" });

            // Récupérer le panier
            Cart = HttpContext.Session.GetObject<List<CartItem>>("Cart") ?? new List<CartItem>();

            // Vérifier si le produit existe déjà
            var existingItem = Cart.Find(p => p.ProductId == item.ProductId);
            if (existingItem != null)
            {
                existingItem.Quantity = existingItem.Quantity + item.Quantity;
            }
            else
            {
                Cart.Add(new CartItem { ProductId = item.ProductId, Name = item.Name, Type = item.Type, SrcImage = item.SrcImage, Quantity = item.Quantity });
            }

            // Sauvegarder dans la session
            HttpContext.Session.SetObject("Cart", Cart);

            return new JsonResult(new { success = true });
        }

        public async Task<IActionResult> OnPostDeleteProduct()
        {
            // Lire le JSON envoyé par JavaScript
            var requestBody = await new System.IO.StreamReader(Request.Body).ReadToEndAsync();
            // Désérialiser le JSON en un objet dynamique
            var jsonObject = JsonSerializer.Deserialize<JsonElement>(requestBody);
            string productIdString = jsonObject.GetProperty("productId").GetString();
            int id;
            int productId = int.TryParse(productIdString, out id) ? id : 0;

            var cart = HttpContext.Session.GetObject<List<CartItem>>("Cart") ?? new List<CartItem>();

            var itemToRemove = cart.FirstOrDefault(p => p.ProductId == productId);
            if (itemToRemove != null)
            {
                cart.Remove(itemToRemove);
                HttpContext.Session.SetObject("Cart", cart);
            }

            return new JsonResult(new { success = true });
        }
    }
}
