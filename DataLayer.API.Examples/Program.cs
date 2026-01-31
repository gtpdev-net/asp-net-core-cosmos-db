using DataLayer.API.Examples.Configuration;
using DataLayer.API.Examples.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Load configuration
var cosmosDbConfig = builder.Configuration
    .GetSection(CosmosDbConfig.SectionName)
    .Get<CosmosDbConfig>() ?? throw new InvalidOperationException("CosmosDb configuration is missing");

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure routing to use lowercase URLs
builder.Services.Configure<RouteOptions>(options =>
{
    options.LowercaseUrls = true;
    options.LowercaseQueryStrings = false;
});

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

// Make the implicit Program class public so test projects can access it
public partial class Program { }
