using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Play.Catalog.Contract;
using Play.Catalog.Service.Dtoss;
using Play.Catalog.Service.Entities;
using Play.Common;

namespace Play.Catalog.Service.Controllers;

[ApiController]
[Route("items")]
public class ItemsController : ControllerBase
{
	private readonly IRepository<Item> _itemsRepository;
	private readonly IPublishEndpoint _publishEndpoint;

	public ItemsController(IRepository<Item> itemsRepository, IPublishEndpoint publishEndpoint)
	{
		_itemsRepository = itemsRepository;
		_publishEndpoint = publishEndpoint;
	}

	[HttpGet]
	public async Task<ActionResult<IEnumerable<ItemDto>>> GetAsync()
	{


		var items = (await _itemsRepository.GetAllAsync()).Select(item => item.AsDto());
		return Ok(items);
	}

	[HttpGet("{id}")]
	public async Task<ActionResult<ItemDto?>> GetByIdAsync(Guid id)
	{
		var item = await _itemsRepository.GetAsync(id);
		if (item == null)
		{
			return NotFound();
		}

		return item.AsDto();
	}

	[HttpPost]
	public async Task<ActionResult<ItemDto>> PostAsync(CreateItemDto createItemDto)
	{
		var item = new Item()
		{
			Name = createItemDto.Name,
			Description = createItemDto.Description,
			Price = createItemDto.Price,
			CreatedDate = DateTimeOffset.UtcNow
		};

		await _itemsRepository.CreateAsync(item);

		await _publishEndpoint.Publish(new CatalogItemCreated(item.Id, item.Name, item.Description));
		return CreatedAtAction(nameof(GetByIdAsync), new { id = item.Id }, item);
	}

	[HttpPut("{id}")]
	public async Task<IActionResult> PutAsync(Guid id, UpdateItemDto updateItemDto)
	{
		var existingItem = await _itemsRepository.GetAsync(id);
		if (existingItem is null) return NotFound();

		existingItem.Name = updateItemDto.Name;
		existingItem.Description = updateItemDto.Description;
		existingItem.Price = updateItemDto.Price;

		await _itemsRepository.UpdateAsync(existingItem);

		await _publishEndpoint.Publish(new CatalogItemUpdated(existingItem.Id, existingItem.Name, existingItem.Description));

		return NoContent();
	}

	[HttpDelete("{id}")]
	public async Task<ActionResult> DeleteAsync(Guid id)
	{
		var item = _itemsRepository.GetAsync(id);
		if (item is null) return NotFound();

		await _itemsRepository.RemoveAsync(id);
		await _publishEndpoint.Publish(new CatalogItemDeleted(id));
		return NoContent();
	}
}
