using Microsoft.AspNetCore.Mvc;
using Play.Common;
using Play.Inventory.Serivce.Clients;
using Play.Inventory.Serivce.Dtos;
using Play.Inventory.Serivce.Entities;

namespace Play.Inventory.Serivce.Controllers;

[ApiController]
[Route("items")]
public class ItemsController : ControllerBase
{
	private readonly IRepository<InventoryItem> _itemsRepository;
	private readonly CatalogClient _catalogClient;

	public ItemsController(IRepository<InventoryItem> itemRepository, CatalogClient catalogClient)
	{
		_itemsRepository = itemRepository;
		_catalogClient = catalogClient;
	}

	[HttpGet]
	public async Task<ActionResult<IEnumerable<InventoryItemDto>>> GetAsync(Guid userId)
	{
		if (userId == Guid.Empty) return BadRequest();

		var catalogItems = await _catalogClient.GetCatalogItemsAsync();
		var inventoryItemEntities = await _itemsRepository.GetAllAsync(item => item.UserId == userId);

		var inventoryItemDtos = inventoryItemEntities.Select(inventoryItem =>
		{
			var catalogItem = catalogItems.Single(catalogItem => catalogItem.Id == inventoryItem.CatalogItemId);
			return inventoryItem.AsDto(catalogItem.Name!, catalogItem.Description!);
		});
		return Ok(inventoryItemDtos); ;
	}

	[HttpPost]
	public async Task<ActionResult> PostAsync(GrantItemsDto itemDto)
	{
		var inventoryItem = await _itemsRepository.GetAsync(item => item.UserId == itemDto.UserId
			&& item.CatalogItemId == itemDto.CatalogItemId);

		if (inventoryItem == null)
		{
			inventoryItem = new InventoryItem()
			{
				CatalogItemId = itemDto.CatalogItemId,
				UserId = itemDto.UserId,
				Quantity = itemDto.Quantity,
				AcquiredDate = DateTimeOffset.UtcNow
			};

			await _itemsRepository.CreateAsync(inventoryItem);
		}
		else
		{
			inventoryItem.Quantity += itemDto.Quantity;
			await _itemsRepository.UpdateAsync(inventoryItem);
		}

		return Ok();
	}
}