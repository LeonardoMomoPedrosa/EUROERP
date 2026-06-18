namespace EUROERP.Application.Orders;

public interface IOrderService
{
    Task<IReadOnlyList<SalesAgentDto>> GetSalesAgentsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LastOrderDto>> GetLastOrdersBySalesAgentAsync(string salesAgentUserName, CancellationToken cancellationToken = default);
    Task<bool> IsClientDelinquentAsync(int clientId, CancellationToken cancellationToken = default);
    Task<int> CreateOrderAsync(CreateOrderDto dto, string applicationId, string userId, CancellationToken cancellationToken = default);

    Task<OrderHeaderDto?> GetOrderHeaderAsync(int orderId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OrderDetailItemDto>> GetOrderDetailsAsync(int orderId, CancellationToken cancellationToken = default);
    Task<ProductForSaleDto?> GetProductForSaleAsync(int productId, int orderId, int clientId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProductForSaleSuggestionDto>> GetProductSuggestionsForSaleAsync(string term, int orderId, int limit = 15, CancellationToken cancellationToken = default);
    /// <param name="unitPriceOverride">When set (positive), stored as line <c>PRICE</c> instead of catalog/market price (POS override).</param>
    Task AddOrderDetailAsync(int orderId, int productId, decimal quantity, int clientId, string applicationId, string userId, decimal? unitPriceOverride = null, CancellationToken cancellationToken = default);
    /// <summary>Add order detail from site import with explicit price (no market lookup). Deducts stock except for <c>Ecom:AnimalTaxLionProductId</c> (fee line is imported without stock movement).</summary>
    Task AddOrderDetailFromSiteAsync(int orderId, int productId, int quantity, decimal price, int clientId, string applicationId, string userId, CancellationToken cancellationToken = default);
    Task RemoveOrderDetailAsync(int orderId, int productId, int clientId, CancellationToken cancellationToken = default);
    Task ResetOrderAsync(int orderId, CancellationToken cancellationToken = default);
    Task UpdateOrderExtraTaxesAsync(int orderId, decimal? discount, decimal? credit, decimal? otherExpenses, decimal? shipmentCost, bool? chargeShipment = null, CancellationToken cancellationToken = default);
    Task UpdateOrderCarKmAndProblemAsync(int orderId, decimal? carKm, string? carProblem, CancellationToken cancellationToken = default);
    Task UpdateOrderCfeAsync(int orderId, string cfeProtocol, string applicationId, string userId, CancellationToken cancellationToken = default);
    Task CancelOrderAsync(int orderId, string applicationId, string userId, CancellationToken cancellationToken = default);

    Task<OrderPaymentSummaryDto?> GetOrderPaymentSummaryAsync(int orderId, CancellationToken cancellationToken = default);

    /// <summary>Loads payment summary row and sets <see cref="OrderPaymentSummaryDto.TotalToPay"/> from <paramref name="precomputedTotalToPay"/> (skips duplicate line-total query).</summary>
    Task<OrderPaymentSummaryDto?> GetOrderPaymentSummaryAsync(int orderId, decimal precomputedTotalToPay, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PaymentMethodOptionDto>> GetAllowedPaymentMethodsAsync(int clientId, decimal totalToPay, CancellationToken cancellationToken = default);
    Task FinishOrderWithPaymentAsync(int orderId, byte paymentMethodId, IReadOnlyList<BtrDetailDto> details, string applicationId, string userId, CancellationToken cancellationToken = default);

    Task ReopenOrderAsync(int orderId, CancellationToken cancellationToken = default);
    Task<OrderForPrintDto?> GetOrderForPrintAsync(int orderId, byte printType, CancellationToken cancellationToken = default);
    Task<OrderLabelDto?> GetOrderLabelAsync(int orderId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OrderSearchResultDto>> SearchOrdersByClientAsync(int clientId, int top = 40, CancellationToken cancellationToken = default);
    Task<OrderSearchResultDto?> SearchOrderByIdAsync(int orderId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OrderSearchResultDto>> SearchOrdersByReceiptAsync(int receiptNumber, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OrderSearchResultDto>> SearchOrdersByMeliAsync(string mlOrderId, CancellationToken cancellationToken = default);
    Task<bool> MlOrderExistsAsync(string mlOrderId, CancellationToken cancellationToken = default);
    Task UnlinkMlOrderAsync(string mlOrderId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MlOrderRowDto>> GetOrdersByMlIdsAsync(IReadOnlyList<string> mlOrderIds, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OrderSearchResultDto>> GetLastClosedOrdersAsync(int top = 30, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PendingOrderDto>> GetPendingOrdersAsync(int? productId, CancellationToken cancellationToken = default);

    Task<SendOrderResult> SendOrderAsync(int orderId, string applicationId, string userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BtrSearchRowDto>> SearchBtrByOrderAsync(int orderId, CancellationToken cancellationToken = default);

    /// <summary>Order summary by id (date, status, NFe key) for external API. Returns null if order not found.</summary>
    Task<OrderByIdDto?> GetOrderByIdAsync(int orderId, CancellationToken cancellationToken = default);

    /// <summary>Client and tracking data by order ID (Epic 14 external API). Returns null if order not found or has no details.</summary>
    Task<ClientByOrderDto?> GetClientByOrderAsync(int orderId, CancellationToken cancellationToken = default);

    /// <summary>Order and client data by NFe key (Epic 14 external API). Returns null if key not found.</summary>
    Task<OrderByNfeKeyDto?> GetOrderByNfeKeyAsync(string nfeKey, CancellationToken cancellationToken = default);

    /// <summary>Last orders with receipt and NFe key that are pending shipment (status E, via null, tracker_ind null, sent in last 7 days). For external shipment tracking.</summary>
    Task<IReadOnlyList<LastNfeOrderDto>> GetLastNfeOrdersPendingShipmentAsync(CancellationToken cancellationToken = default);

    /// <summary>Updates the VIA column for an order (external API for shipment tracking).</summary>
    Task<bool> UpdateOrderViaAsync(int orderId, string via, CancellationToken cancellationToken = default);

    /// <summary>Updates Track and/or Via for an order (external API PUT .../via), sets TRACKER_IND to P (processed). At least one of track or via must be non-null.</summary>
    Task<bool> UpdateOrderTrackAsync(int orderId, string? track, string? via, CancellationToken cancellationToken = default);
}
