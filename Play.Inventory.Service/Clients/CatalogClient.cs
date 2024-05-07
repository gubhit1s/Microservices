using Play.Inventory.Serivce.Dtos;

namespace Play.Inventory.Serivce.Clients;

public class CatalogClient
{
	private readonly HttpClient _httpClient;

	public CatalogClient(HttpClient httpClient)
	{
		_httpClient = httpClient;
	}

	public async Task<IReadOnlyCollection<CatalogItemDto>> GetCatalogItemsAsync()
	{
		var items = await _httpClient.GetFromJsonAsync<IReadOnlyCollection<CatalogItemDto>>("/items");
		return items!;
	}
}