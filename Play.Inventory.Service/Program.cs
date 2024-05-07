using Microsoft.Extensions.DependencyInjection;
using Play.Common.MongoDB;
using Play.Inventory.Serivce.Clients;
using Play.Inventory.Serivce.Entities;
using Polly;
using Polly.Timeout;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();

builder.Services.AddMongo()
	.AddMongoRepository<InventoryItem>("invetoryitems");

Random jitterer = new Random();

builder.Services.AddHttpClient<CatalogClient>(client =>
{
	client.BaseAddress = new Uri("https://localhost:7121");
})
	.AddTransientHttpErrorPolicy(config => config.Or<TimeoutRejectedException>().WaitAndRetryAsync(5,
		retryAttempt => TimeSpan.FromSeconds(Math.Pow(retryAttempt, 2))
			+ TimeSpan.FromMilliseconds(jitterer.Next(0, 1000)),
		onRetry: (outcome, timespan, retryAttempt) =>
		{
			var serviceProvider = builder.Services.BuildServiceProvider();
			serviceProvider.GetService<ILogger<CatalogClient>>()?
				.LogWarning($"Delaying for {timespan.TotalSeconds} seconds, then making retry #${retryAttempt}.");
		}))
	.AddTransientHttpErrorPolicy(config => config.Or<TimeoutRejectedException>().CircuitBreakerAsync(
		handledEventsAllowedBeforeBreaking: 3,
		durationOfBreak: TimeSpan.FromSeconds(15),
		onBreak: (outcome, timespan) =>
		{
			var serviceProvider = builder.Services.BuildServiceProvider();
			serviceProvider.GetService<ILogger<CatalogClient>>()?
				.LogWarning($"Opening the circuit for {timespan.TotalSeconds}");
		},
		onReset: () =>
		{
			var serviceProvider = builder.Services.BuildServiceProvider();
			serviceProvider.GetService<ILogger<CatalogClient>>()?
				.LogWarning($"Closing the circuit...");
		}
	))
  	.AddPolicyHandler(Policy.TimeoutAsync<HttpResponseMessage>(1));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();
