namespace EUROERP.Application.NFe;

/// <summary>Input for one order when saving the schedule batch (Agendar).</summary>
public class ScheduleItemInput
{
    public int OrderId { get; set; }
    public bool IsSelected { get; set; }
    public decimal Discount { get; set; }
}
