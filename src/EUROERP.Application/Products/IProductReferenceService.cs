namespace EUROERP.Application.Products;

public interface IProductReferenceService
{
    Task<IReadOnlyList<IdNameDto>> GetMarketsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<IdNameDto>> GetProductClassesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<IdNameDto>> GetProductGroupsByClassAsync(byte classId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<IdNameDto>> GetAllProductGroupsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<IdNameDto>> GetFiscalClassesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<IdNameDto>> GetCurrenciesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<IdNameDto>> GetCstAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<IdNameDto>> GetCstbAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<IdNameDto>> GetAnimalSizesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<IdNameDto>> GetSuppliersAsync(CancellationToken cancellationToken = default);

    /// <summary>Suppliers that have at least one product (PRODUCT_SUPPLIER_LINK). For ABC Consolidado filter dropdown (same as legacy getProductSuppliersListForDropDown).</summary>
    Task<IReadOnlyList<IdNameDto>> GetProductSuppliersForDropDownAsync(CancellationToken cancellationToken = default);
}
