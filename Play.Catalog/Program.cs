using MassTransit;
using Microsoft.Extensions.Options;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using Play.Catalog.Service.Entities;
using Play.Catalog.Service.Settings;
using Play.Common;
using Play.Common.MongoDB;
using Play.Common.Settings;

var builder = WebApplication.CreateBuilder(args);


// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var serviceSettings = builder.Configuration.GetSection(nameof(ServiceSettings)).Get<ServiceSettings>();


builder.Services.AddMongo();
builder.Services.AddMongoRepository<Item>("items");
builder.Services.AddMassTransit(x =>
{
	x.UsingRabbitMq((context, configurator) =>
	{
		var rabbitMQSettings = builder.Configuration.GetSection(nameof(RabbitMQSettings)).Get<RabbitMQSettings>();
		configurator.Host(rabbitMQSettings!.Host);
		configurator.ConfigureEndpoints(context, new KebabCaseEndpointNameFormatter(serviceSettings!.ServiceName,
		 includeNamespace: false));
	});
});

builder.Services.AddControllers(options =>
{
	options.SuppressAsyncSuffixInActionNames = false;
});


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
