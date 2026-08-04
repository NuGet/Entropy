namespace InventoryApp;

public sealed class InventoryItem
{
    public int Id { get; set; }

    public required string Sku { get; set; }

    public required string Name { get; set; }

    public int Quantity { get; set; }
}
