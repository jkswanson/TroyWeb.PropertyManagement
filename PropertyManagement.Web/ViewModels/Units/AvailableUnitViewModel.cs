namespace PropertyManagement.Web.ViewModels.Units;

public class AvailableUnitViewModel
{
    public int Id { get; set; }
    public string PropertyName { get; set; } = string.Empty;
    public string PropertyAddress { get; set; } = string.Empty;
    public string UnitNumber { get; set; } = string.Empty;
    public string UnitTypeName { get; set; } = string.Empty;
    public int Bedrooms { get; set; }
    public decimal MonthlyRent { get; set; }
}
