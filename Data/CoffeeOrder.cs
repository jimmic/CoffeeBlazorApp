namespace CoffeeBlazorApp.Data;

public sealed class CoffeeOrder
{
    public int Id { get; set; }

    public string OrderNumber { get; set; } = string.Empty;

    public string TrackingToken { get; set; } = string.Empty;

    public string CoffeeType { get; set; } = string.Empty;

    public string Size { get; set; } = string.Empty;

    public OrderStatus Status { get; set; } = OrderStatus.Queued;

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? ReadyAtUtc { get; set; }
}
