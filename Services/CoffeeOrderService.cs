using System.Security.Cryptography;
using CoffeeBlazorApp.Data;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace CoffeeBlazorApp.Services;

public sealed class CoffeeOrderService(
    IDbContextFactory<CoffeeDbContext> contextFactory,
    IHubContext<CoffeeOrderHub> hubContext)
{
    private static readonly string[] CoffeeTypes = ["Americano", "Cappuccino", "Latte", "Mocha"];
    private static readonly string[] Sizes = ["Small", "Medium", "Large"];

    public IReadOnlyList<string> AvailableCoffeeTypes => CoffeeTypes;

    public IReadOnlyList<string> AvailableSizes => Sizes;

    public async Task<CoffeeOrder> CreateOrderAsync(string coffeeType, string size, CancellationToken cancellationToken = default)
    {
        if (!CoffeeTypes.Contains(coffeeType, StringComparer.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Select a valid coffee type.", nameof(coffeeType));
        }

        if (!Sizes.Contains(size, StringComparer.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Select a valid size.", nameof(size));
        }

        var order = new CoffeeOrder
        {
            OrderNumber = $"{RandomNumberGenerator.GetInt32(1000, 10000)}",
            TrackingToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(16)),
            CoffeeType = coffeeType,
            Size = size,
            CreatedAtUtc = DateTime.UtcNow
        };

        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        db.Orders.Add(order);
        await db.SaveChangesAsync(cancellationToken);
        await hubContext.Clients.All.SendAsync("OrderCreated", ToSummary(order), cancellationToken);

        return order;
    }

    public async Task<IReadOnlyList<CoffeeOrder>> GetActiveOrdersAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await db.Orders
            .Where(order => order.Status != OrderStatus.Completed)
            .OrderBy(order => order.CreatedAtUtc)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<CoffeeOrder?> GetOrderAsync(string trackingToken, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await db.Orders
            .AsNoTracking()
            .SingleOrDefaultAsync(order => order.TrackingToken == trackingToken, cancellationToken);
    }

    public async Task<CoffeeOrder> AdvanceStatusAsync(int orderId, OrderStatus nextStatus, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var order = await db.Orders.SingleOrDefaultAsync(item => item.Id == orderId, cancellationToken)
            ?? throw new InvalidOperationException("The order no longer exists.");

        if (!IsValidTransition(order.Status, nextStatus))
        {
            throw new InvalidOperationException($"An order cannot move from {order.Status} to {nextStatus}.");
        }

        order.Status = nextStatus;
        if (nextStatus == OrderStatus.Ready)
        {
            order.ReadyAtUtc = DateTime.UtcNow;
        }

        await db.SaveChangesAsync(cancellationToken);
        await hubContext.Clients.All.SendAsync("OrderStatusChanged", ToSummary(order), cancellationToken);
        return order;
    }

    private static bool IsValidTransition(OrderStatus current, OrderStatus next) =>
        (current, next) switch
        {
            (OrderStatus.Queued, OrderStatus.Preparing) => true,
            (OrderStatus.Preparing, OrderStatus.Ready) => true,
            (OrderStatus.Ready, OrderStatus.Completed) => true,
            _ => false
        };

    private static CoffeeOrderSummary ToSummary(CoffeeOrder order) => new(
        order.Id,
        order.OrderNumber,
        order.TrackingToken,
        order.CoffeeType,
        order.Size,
        order.Status);
}

public sealed record CoffeeOrderSummary(
    int Id,
    string OrderNumber,
    string TrackingToken,
    string CoffeeType,
    string Size,
    OrderStatus Status);
