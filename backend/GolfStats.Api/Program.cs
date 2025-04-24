using GolfStats.Api.Data;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddSerilog(o => o.WriteTo.Console());
builder.Services.AddControllers();;

builder.Services.AddDbContext<GolfStatsDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("GolfStatsContext")));

builder.Services.AddHybridCache();

var app = builder.Build();

app.UseSerilogRequestLogging();

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<GolfStatsDbContext>().Database.EnsureCreated();
}

app.Run();
