using EUROERP.Application.Products;

namespace EUROERP.Application.Clients;

public interface IClientReferenceService
{
    Task<IReadOnlyList<IdNameDto>> GetMarketsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<IdNameDto>> GetCountriesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<IdNameDto>> GetStatesAsync(int countryId = 1, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<IdNameDto>> GetCitiesByStateAsync(byte stateId, CancellationToken cancellationToken = default);
    /// <summary>Payment methods from <c>PAYMENT_METHOD</c> with <c>MAX_TERMS</c> / <c>MIN_AMOUNT</c> (legacy SOAP parity for SL4Box).</summary>
    Task<IReadOnlyList<PaymentMethodRefDto>> GetPaymentMethodsAsync(CancellationToken cancellationToken = default);
    /// <summary>Payment method by id; <c>null</c> if negative or missing in DB. Id <c>0</c> is the list placeholder (<c>Selecione</c>).</summary>
    Task<PaymentMethodRefDto?> GetPaymentMethodByIdAsync(int id, CancellationToken cancellationToken = default);
    /// <summary>Sub-methods for a payment method (<c>PAYMENT_SUB_METHOD.PAYMENT_METHOD_ID</c>). Empty when none; does not validate parent exists.</summary>
    Task<IReadOnlyList<PaymentSubMethodDto>> GetPaymentSubMethodsAsync(int paymentMethodId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<IdNameDto>> GetDeliverySuppliersAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UserIdNameDto>> GetSalesAgentsAsync(CancellationToken cancellationToken = default);
    Task<byte?> GetStateIdByCodeAsync(string uf, CancellationToken cancellationToken = default);
    Task<byte?> GetStateIdByNameAsync(string stateName, CancellationToken cancellationToken = default);
}
