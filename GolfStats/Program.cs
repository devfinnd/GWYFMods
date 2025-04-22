using GolfStats.Controllers;
using GolfStats.Data;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Serilog;
using ILogger = Serilog.ILogger;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddSerilog(o => o.WriteTo.Console());
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<GolfStatsDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("GolfStatsContext")));

builder.Services.AddHybridCache();
builder.Services.AddHttpLogging(o => o.LoggingFields = HttpLoggingFields.All | HttpLoggingFields.RequestBody | HttpLoggingFields.ResponseBody);

var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseHttpLogging();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

app.MapControllers();


using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<GolfStatsDbContext>().Database.EnsureCreated();
}

app.Run();
