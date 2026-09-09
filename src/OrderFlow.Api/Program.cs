// Program.cs
using Asp.Versioning;
using Microsoft.EntityFrameworkCore;
//using Microsoft.Extensions.Options;
using OrderFlow.Api.Extensions;
using OrderFlow.Api.Infrastructure;
using OrderFlow.Application;
using OrderFlow.Infrastructure;
using OrderFlow.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddScoped<IdempotencyFilter>();

builder.Services.AddApiVersioning(o =>
{
    o.DefaultApiVersion = new ApiVersion(1, 0);
    o.ApiVersionReader = new UrlSegmentApiVersionReader();
    //o.AssumeDefaultVersionWhenUnspecified = true;
    //o.AssumeDefaultVersionWhenUnspecified = true;
    o.ReportApiVersions = true;
}).AddApiExplorer(o =>
{
    o.GroupNameFormat = "'v'VVV";
    o.SubstituteApiVersionInUrl = true;
}).AddMvc();

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHttpClient("resilient").AddStandardResilienceHandler(); 

builder.Services.AddOutputCache();
builder.Services.AddResponseCompression();

builder.Services.AddAppHealthChecks(builder.Configuration);

var app = builder.Build();

app.UseExceptionHandler();
app.UseResponseCompression();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<OrderDbContext>().Database.MigrateAsync();
}

app.UseOutputCache();
app.MapControllers();
app.MapAppHealthChecks();  

app.Run();

public partial class Program; 
