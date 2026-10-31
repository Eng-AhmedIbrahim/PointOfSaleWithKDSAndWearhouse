namespace POS.Contract.Dtos.SettingsDtos;

public class HqSettingDto
{
    public int Id { get; set; }
    public string ServerName { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool UseIntegratedSecurity { get; set; }
    public bool IsEnabled { get; set; }
    public DateTime? LastSyncDate { get; set; }
    public int ConnectTimeout { get; set; } = 15;
    public bool TrustServerCertificate { get; set; } = true;
    public bool Encrypt { get; set; } = true;
}
