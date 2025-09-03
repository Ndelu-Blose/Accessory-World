namespace AccessoryWorld.Models
{
    public class AddToCartRequest
    {
        public int ProductId { get; set; }
        public int SKUId { get; set; }
        public int Quantity { get; set; }
    }

    public class UpdateCartItemRequest
    {
        public int CartItemId { get; set; }
        public int Quantity { get; set; }
    }

    public class RemoveCartItemRequest
    {
        public int CartItemId { get; set; }
    }
}