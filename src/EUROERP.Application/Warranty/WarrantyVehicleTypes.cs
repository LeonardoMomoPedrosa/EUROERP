namespace EUROERP.Application.Warranty;

public static class WarrantyVehicleTypes
{
    public static IReadOnlyList<(string Value, string Label)> Options { get; } =
    [
        ("T", "Caminhão"),
        ("O", "Onibus"),
        ("C", "Carro")
    ];

    public static string GetLabel(string? code) =>
        Options.FirstOrDefault(o => o.Value == code).Label ?? code ?? "-";
}
