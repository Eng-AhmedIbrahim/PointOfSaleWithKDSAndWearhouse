using POS.Core.Entities.Kitchen;
using POS.Core.Services.Contract.PrintingSettings;
using Pos.Repository.Data.DataSeed;

namespace POS.Services.PrintingSettings;

public class PrintingSettingsService : IPrintingSettingsServices
{
    private readonly IUnitOfWork _unitOfWork;

    public PrintingSettingsService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<POS.Core.Entities.Kitchen.PrintingSettings>> GetSettingsByComputerNameAsync(string computerName)
    {
        var spec = new POS.Core.Specifications.BaseSpecifications<POS.Core.Entities.Kitchen.PrintingSettings>(s => s.ComputerName == computerName);
        var settings = await _unitOfWork.Repository<POS.Core.Entities.Kitchen.PrintingSettings>().GetAllWithSpecificationAsync(spec);

        if (settings == null || !settings.Any())
        {
            await InitializeSettingsForComputerAsync(computerName);
            settings = await _unitOfWork.Repository<POS.Core.Entities.Kitchen.PrintingSettings>().GetAllWithSpecificationAsync(spec);
        }

        return settings.ToList();
    }

    public async Task<bool> InitializeSettingsForComputerAsync(string computerName)
    {
        // Try to fetch KitchenTypes to create default settings for each kitchen type
        var kitchenTypes = await _unitOfWork.Repository<KitchenType>().GetAllAsync();
        
        if (kitchenTypes != null && kitchenTypes.Any())
        {
            foreach (var kitchen in kitchenTypes)
            {
                var newSetting = new POS.Core.Entities.Kitchen.PrintingSettings
                {
                    KitchenId = kitchen.Id,
                   // Kitchen = kitchen, // Don't set navigation property directly for Add
                    ComputerName = computerName,
                    BranchID = kitchen.BranchId,
                    OrderType = "DineIn", // Default or should be configurable
                    Copy1 = "1", // Default Copy
                    OutLetType = "Kitchen" 
                };
                await _unitOfWork.Repository<POS.Core.Entities.Kitchen.PrintingSettings>().AddAsync(newSetting);
            }
            return await _unitOfWork.CompleteAsync() > 0;
        }

        return false;
    }

    public async Task<bool> UpdateSettingsAsync(List<POS.Core.Entities.Kitchen.PrintingSettings> settings)
    {
        if (settings == null || !settings.Any()) return false;

        foreach (var setting in settings)
        {
            var existing = await _unitOfWork.Repository<POS.Core.Entities.Kitchen.PrintingSettings>().GetByIdAsync(setting.Id);
            if (existing != null)
            {
                existing.Copy1 = setting.Copy1;
                existing.Copy2 = setting.Copy2;
                existing.Copy3 = setting.Copy3;
                existing.Copy4 = setting.Copy4;
                existing.Copy5 = setting.Copy5;
                existing.OrderType = setting.OrderType;
                existing.OutLetType = setting.OutLetType;
                // Don't update ComputerName or KitchenId usually
                
                _unitOfWork.Repository<POS.Core.Entities.Kitchen.PrintingSettings>().Update(existing);
            }
            else
            {
                await _unitOfWork.Repository<POS.Core.Entities.Kitchen.PrintingSettings>().AddAsync(setting);
            }
        }

        return await _unitOfWork.CompleteAsync() > 0;
    }
}
