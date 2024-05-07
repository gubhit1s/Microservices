using Play.Inventory.Serivce.Dtos;
using Play.Inventory.Serivce.Entities;

namespace Play.Inventory.Serivce;

public static class Extensions
{
	public static InventoryItemDto AsDto(this InventoryItem item, string name, string description)
	{
		return new InventoryItemDto(item.CatalogItemId, name, description, item.Quantity, item.AcquiredDate);
	}
}