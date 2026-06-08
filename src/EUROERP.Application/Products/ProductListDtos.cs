namespace EUROERP.Application.Products;

public enum ProductListMode
{
    /// <summary>Preços — todo estoque (Eurobus RadioButton_price_all).</summary>
    PriceAll,
    /// <summary>Preços — apenas estoque (Eurobus RadioButton_price_stock).</summary>
    PriceInStock,
    /// <summary>Lista de saldos (Eurobus RadioButton_stock).</summary>
    Stock
}

public class ProductListRequest
{
    public ProductListMode Mode { get; set; } = ProductListMode.PriceAll;
    public bool AllGroups { get; set; }
    public IReadOnlyList<byte> GroupIds { get; set; } = Array.Empty<byte>();
}

public class ProductListItemDto
{
    public int Id { get; set; }
    public string LionCode { get; set; } = string.Empty;
    public string? ExternalPkid { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public decimal Stock { get; set; }
    public string? UnitLabel { get; set; }
    public bool DecimalStock { get; set; }
}

public class ProductListGroupDto
{
    public byte GroupId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public List<ProductListItemDto> Items { get; set; } = new();
}

public class ProductListExportDto
{
    public string Title { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public ProductListMode Mode { get; set; }
    public List<ProductListGroupDto> Groups { get; set; } = new();
}
