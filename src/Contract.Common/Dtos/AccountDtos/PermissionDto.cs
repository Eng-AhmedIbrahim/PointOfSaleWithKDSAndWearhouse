namespace POS.Contract.Dtos.AccountDtos;

public class PermissionDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string PoliceArabicName { get; set; } = string.Empty;
    public string PoliceEnglishNameEn { get; set; } = string.Empty;
    public string ScreenArabicName { get; set; } = string.Empty;
    public string ScreenEnglishName { get; set; } = string.Empty;
    public bool IsBackOffice { get; set; }
    public bool IsWarehouse { get; set; }
}

public class RolePermissionDto
{
    public string RoleId { get; set; } = string.Empty;
    public int PermissionId { get; set; }
}

public class RolePermissionItemDto
{
    public string Permission { get; set; } = string.Empty;
    public bool IsGranted { get; set; }
}
