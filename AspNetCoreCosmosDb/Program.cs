using AspNetCoreCosmosDb.Configuration;
using AspNetCoreCosmosDb.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Load configuration
var cosmosDbConfig = builder.Configuration
    .GetSection(CosmosDbConfig.SectionName)
    .Get<CosmosDbConfig>() ?? throw new InvalidOperationException("CosmosDb configuration is missing");

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add infrastructure services
builder.Services.AddInfrastructureExtensions(builder.Configuration, cosmosDbConfig);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
