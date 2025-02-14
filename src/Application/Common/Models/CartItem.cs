
namespace Application.Common.Models;

/// <summary>
/// CartItem class
/// </summary>
public class CartItem
{
    public int ProductId { get; set; }
    public string Name { get; set; }
    public string Type { get; set; }
    public string SrcImage { get; set; }
    public int Quantity { get; set; }
}
