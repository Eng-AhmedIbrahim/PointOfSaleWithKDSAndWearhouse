namespace POS.Contract.Dtos.SettingsDtos;

public class SyncRequestDto
{
    public bool SyncMasterData { get; set; }
    public bool SyncSalesItems { get; set; } // Includes Categories and Attributes
    public bool SyncEmployees { get; set; }
    public bool SyncStoreItems { get; set; }
    public bool SyncSuppliers { get; set; }
    public bool SyncWarehouses { get; set; }
}
