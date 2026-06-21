using EFCoreInternals;
using EFCoreInternals.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

HostApplicationBuilder builder = Host.CreateApplicationBuilder();

Directory.CreateDirectory("data"); // no-op to ensure data/ folder exists
builder.Services.AddDbContext<OrderDbContext>(options =>
  options.UseSqlite("Data Source=./data/orders.db")
          .UseLazyLoadingProxies()
          .EnableSensitiveDataLogging()
          .LogTo(Console.WriteLine, LogLevel.Information));

IHost host = builder.Build();

await using AsyncServiceScope scope = host.Services.CreateAsyncScope();
IServiceProvider scopedServiceProvider = scope.ServiceProvider;
OrderDbContext db = scopedServiceProvider.GetRequiredService<OrderDbContext>();

if (!await OrderDbSeeder.IsSeeded(db))
{
  await OrderDbSeeder.SeedAsync(db);
  db.ChangeTracker.Clear(); // start exercises with a clean tracker
}

await OrderExercises.TrackingVsNoTracking(db);
await OrderExercises.LazyLoadingAndN1(db);
await OrderExercises.IncludeChainsAndSplitQuery(db);
