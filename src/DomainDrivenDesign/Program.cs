// DDD exercise runner — exercises wired up here as they are completed
using DomainDrivenDesign;
using DomainDrivenDesign.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

HostApplicationBuilder builder = Host.CreateApplicationBuilder();

Directory.CreateDirectory("data"); // no-op to ensure data/ folder exists
builder.Services.AddDbContext<OrderDbContext>(options =>
  options.UseSqlite("Data Source=./data/ddd_orders.db")
          .EnableSensitiveDataLogging()
          .LogTo(Console.WriteLine, LogLevel.Information));

IHost host = builder.Build();

await using AsyncServiceScope scope = host.Services.CreateAsyncScope();
IServiceProvider scopedServiceProvider = scope.ServiceProvider;
OrderDbContext db = scopedServiceProvider.GetRequiredService<OrderDbContext>();

await db.Database.EnsureCreatedAsync();

await DddExercises.AggregateAndRoot(db);
await DddExercises.BoundedContextSnapshot(db);
await DddExercises.AnemicVsRich(db);
