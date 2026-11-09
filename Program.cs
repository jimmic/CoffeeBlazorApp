using CoffeeBlazorApp.Components;
using CoffeeBlazorApp.Data;
using CoffeeBlazorApp.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddDbContextFactory<CoffeeDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("CoffeeOrders")));
builder.Services.AddScoped<CoffeeOrderService>();
builder.Services.AddSignalR();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

await using (var scope = app.Services.CreateAsyncScope())
{
    var contextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<CoffeeDbContext>>();
    await using var db = await contextFactory.CreateDbContextAsync();
    await db.Database.EnsureCreatedAsync();
}

app.MapStaticAssets();
app.MapHub<CoffeeOrderHub>("/hubs/orders");
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
