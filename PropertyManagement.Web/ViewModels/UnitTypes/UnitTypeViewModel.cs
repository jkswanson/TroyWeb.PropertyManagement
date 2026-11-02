namespace PropertyManagement.Web.ViewModels.UnitTypes;

public class UnitTypeViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int UnitCount { get; set; }
}

