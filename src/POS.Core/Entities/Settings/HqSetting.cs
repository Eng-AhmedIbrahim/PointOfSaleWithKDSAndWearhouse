using Microsoft.Data.SqlClient;
using POS.Core.Entities;

namespace POS.Core.Entities.Settings;

public class HqSetting : BaseEntity
{
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

    public string GetConnectionString()
    {
        var builder = new SqlConnectionStringBuilder
        {
            DataSource = ServerName,
            InitialCatalog = DatabaseName,
            IntegratedSecurity = UseIntegratedSecurity,
            ConnectTimeout = ConnectTimeout,
            TrustServerCertificate = TrustServerCertificate,
            Encrypt = Encrypt
        };

        if (!UseIntegratedSecurity)
        {
            builder.UserID = UserName;
            builder.Password = Password;
        }

        return builder.ConnectionString;
    }
}
