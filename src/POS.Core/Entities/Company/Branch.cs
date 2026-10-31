namespace POS.Core.Entities.Company;

public class Branch : BaseEntity
{
    public string? Name { get; set; } 
    public string? NormalizedName { get; set; } 
    public string? Description { get; set; } 
    public string? ImagePath { get; set; }
    public int LogoWidth { get; set; } = 200;
    public int LogoHeight { get; set; } = 100;
    public string? Address { get; set; } 
    public string? Phone1 { get; set; } 
    public string? Phone2 { get; set; } 
    public bool Active { get; set; } = true;
    public bool Suspend { get; set; } = false;
    public DateTime CreationDate { get; set; }
    public DateTime? UpdateDate { get; set; }
    public string? Currency { get; set; } = "EGP";

    public string? ApiUrl { get; set; }
    public int? CompanyId { get; set; }
    public Company? Company { get; set; }

    public ICollection<CustomerAddress>? CustomerAddresses { get; set; }
    public ICollection<DeliveryZone>? DeliveryZones { get; set; }
}