namespace POS.Services.StaffMealServices
{
    public class StaffMealService : IStaffMealService
    {
        private readonly IUnitOfWork _unitOfWork;

        public StaffMealService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<StaffMealConfigDto?> GetConfigByUserIdAsync(string userId)
        {
            var configs = await _unitOfWork.Repository<StaffMealConfig>().GetAllAsync();
            var config = configs.FirstOrDefault(c => c.UserId == userId && c.IsActive);
            
            if (config == null) return null;

            var dto = new StaffMealConfigDto
            {
                Id = config.Id,
                UserId = config.UserId,
                UserName = config.UserName,
                ItemId = config.ItemId?.ToString(),
                ItemName = config.ItemName,
                CategoryId = config.CategoryId,
                CategoryName = config.CategoryName,
                GroupId = config.GroupId,
                GroupName = config.GroupName,
                DailyLimit = config.DailyLimit,
                DailyAmountLimit = config.DailyAmountLimit,
                IsAllItemsAllowed = config.IsAllItemsAllowed,
                MealLimit = config.MealLimit,
                SpecialPrice = config.SpecialPrice,
                IsActive = config.IsActive
            };

            // If it's a specific item and category is missing, try to fill it
            if (dto.CategoryId == null && !string.IsNullOrEmpty(dto.ItemId))
            {
                if (int.TryParse(dto.ItemId, out var itemId))
                {
                    var item = await _unitOfWork.Repository<MenuSalesItems>().GetByIdAsync(itemId);
                    dto.CategoryId = item?.CategoryId;
                }
            }

            return dto;
        }

        public async Task<StaffMealStatusDto> GetStatusByUserIdAsync(string userId)
        {
            var config = await GetConfigByUserIdAsync(userId);
            if (config == null) return new StaffMealStatusDto { IsEligible = false };

            // Find all usage today
            var usages = await _unitOfWork.Repository<StaffMealUsage>().GetAllAsync();
            var todayUsages = usages.Where(u => u.UserId == userId && u.Date.Date == DateTime.Today).ToList();
            
            // Count distinct sessions. Multiple items in the same OrderId count as 1 usage.
            var sessionCount = todayUsages.Where(u => u.OrderId.HasValue).Select(u => u.OrderId!.Value).Distinct().Count()
                             + todayUsages.Count(u => !u.OrderId.HasValue);

            // Calculate spent amount based on original item prices
            decimal totalSpentToday = 0;
            var allItems = await _unitOfWork.Repository<MenuSalesItems>().GetAllAsync();
            foreach (var usage in todayUsages)
            {
                var item = allItems.FirstOrDefault(i => i.Id == usage.ItemId);
                if (item != null) totalSpentToday += item.Price ?? 0;
            }

            decimal effectiveAmountLimit = config.DailyAmountLimit;
            bool effectiveAllItems = config.IsAllItemsAllowed;
            int effectiveDailyLimit = config.DailyLimit;
            int effectiveMealLimit = config.MealLimit;
            
            if (config.GroupId.HasValue)
            {
                var group = await _unitOfWork.Repository<StaffMealGroup>().GetByIdAsync(config.GroupId.Value);
                if (group != null)
                {
                    if (effectiveAmountLimit == 0) effectiveAmountLimit = group.DailyAmountLimit;
                    if (!effectiveAllItems) effectiveAllItems = group.IsAllItemsAllowed;
                    if (effectiveDailyLimit == 0) effectiveDailyLimit = group.DailyLimit; // Assuming you added these to group entity too
                    if (effectiveMealLimit == 0) effectiveMealLimit = group.MealLimit;
                }
            }

            // Sync effective values
            config.DailyAmountLimit = effectiveAmountLimit;
            config.IsAllItemsAllowed = effectiveAllItems;
            config.DailyLimit = effectiveDailyLimit;
            config.MealLimit = effectiveMealLimit;

            bool isAmountMode = effectiveAmountLimit > 0;

            return new StaffMealStatusDto
            {
                IsEligible = isAmountMode 
                    ? (totalSpentToday < effectiveAmountLimit) 
                    : (sessionCount < config.DailyLimit),
                RemainingToday = isAmountMode ? 999 : Math.Max(0, config.DailyLimit - sessionCount),
                RemainingAmountToday = !isAmountMode ? 0 : Math.Max(0, effectiveAmountLimit - totalSpentToday),
                Config = config
            };
        }

        public async Task<bool> RecordUsageAsync(StaffMealUsageDto usage)
        {
            var entity = new StaffMealUsage
            {
                UserId = usage.UserId!,
                ItemId = int.Parse(usage.ItemId!),
                Date = DateTime.Now,
                OrderId = int.TryParse(usage.OrderId, out int oid) ? oid : (int?)null
            };

            await _unitOfWork.Repository<StaffMealUsage>().AddAsync(entity);
            return await _unitOfWork.CompleteAsync() > 0;
        }

        public async Task<IEnumerable<StaffMealConfigDto>> GetAllConfigsAsync()
        {
            var configs = await _unitOfWork.Repository<StaffMealConfig>().GetAllAsync();
            var allItems = await _unitOfWork.Repository<MenuSalesItems>().GetAllAsync();
            var itemCatMap = allItems.ToDictionary(i => i.Id, i => i.CategoryId);

            return configs.Select(c => {
                var dto = new StaffMealConfigDto
                {
                    Id = c.Id,
                    UserId = c.UserId,
                    UserName = c.UserName,
                    ItemId = c.ItemId?.ToString(),
                    ItemName = c.ItemName,
                    CategoryId = c.CategoryId,
                    CategoryName = c.CategoryName,
                    GroupId = c.GroupId,
                    GroupName = c.GroupName,
                    DailyLimit = c.DailyLimit,
                    DailyAmountLimit = c.DailyAmountLimit,
                    IsAllItemsAllowed = c.IsAllItemsAllowed,
                    MealLimit = c.MealLimit,
                    SpecialPrice = c.SpecialPrice,
                    IsActive = c.IsActive
                };
                if (dto.CategoryId == null && !string.IsNullOrEmpty(dto.ItemId))
                {
                    if (int.TryParse(dto.ItemId, out var iid) && itemCatMap.TryGetValue(iid, out var cid))
                        dto.CategoryId = cid;
                }
                return dto;
            });
        }

        public async Task<bool> UpsertConfigAsync(StaffMealConfigDto config)
        {
            if (config.Id > 0)
            {
                var existing = await _unitOfWork.Repository<StaffMealConfig>().GetByIdAsync(config.Id);
                if (existing == null) return false;

                existing.UserId = config.UserId!;
                existing.UserName = config.UserName;
                existing.ItemId = int.TryParse(config.ItemId, out int iid) ? iid : (int?)null;
                existing.ItemName = config.ItemName;
                existing.CategoryId = config.CategoryId;
                existing.CategoryName = config.CategoryName;
                existing.GroupId = config.GroupId;
                existing.GroupName = config.GroupName;
                existing.DailyLimit = config.DailyLimit;
                existing.DailyAmountLimit = config.DailyAmountLimit;
                existing.IsAllItemsAllowed = config.IsAllItemsAllowed;
                existing.MealLimit = config.MealLimit;
                existing.SpecialPrice = config.SpecialPrice;
                existing.IsActive = config.IsActive;

                _unitOfWork.Repository<StaffMealConfig>().Update(existing);
            }
            else
            {
                // Prevent duplicates: Check if user already has a setting
                var all = await _unitOfWork.Repository<StaffMealConfig>().GetAllAsync();
                var existingByUserId = all.FirstOrDefault(x => x.UserId == config.UserId);

                if (existingByUserId != null)
                {
                    existingByUserId.UserName = config.UserName;
                    existingByUserId.ItemId = int.TryParse(config.ItemId, out int iid2) ? iid2 : (int?)null;
                    existingByUserId.ItemName = config.ItemName;
                    existingByUserId.CategoryId = config.CategoryId;
                    existingByUserId.CategoryName = config.CategoryName;
                    existingByUserId.GroupId = config.GroupId;
                    existingByUserId.GroupName = config.GroupName;
                    existingByUserId.DailyLimit = config.DailyLimit;
                    existingByUserId.DailyAmountLimit = config.DailyAmountLimit;
                    existingByUserId.IsAllItemsAllowed = config.IsAllItemsAllowed;
                    existingByUserId.MealLimit = config.MealLimit;
                    existingByUserId.SpecialPrice = config.SpecialPrice;
                    existingByUserId.IsActive = config.IsActive;
                    _unitOfWork.Repository<StaffMealConfig>().Update(existingByUserId);
                }
                else
                {
                    var entity = new StaffMealConfig
                    {
                        UserId = config.UserId!,
                        UserName = config.UserName,
                        ItemId = int.TryParse(config.ItemId, out int iid) ? iid : (int?)null,
                        ItemName = config.ItemName,
                        CategoryId = config.CategoryId,
                        CategoryName = config.CategoryName,
                        GroupId = config.GroupId,
                        GroupName = config.GroupName,
                        DailyLimit = config.DailyLimit,
                        DailyAmountLimit = config.DailyAmountLimit,
                        IsAllItemsAllowed = config.IsAllItemsAllowed,
                        MealLimit = config.MealLimit,
                        SpecialPrice = config.SpecialPrice,
                        IsActive = config.IsActive
                    };
                    await _unitOfWork.Repository<StaffMealConfig>().AddAsync(entity);
                }
            }

            await _unitOfWork.CompleteAsync();
            return true;
        }

        public async Task<bool> BatchUpsertConfigsAsync(IEnumerable<StaffMealConfigDto> configs)
        {
            var all = await _unitOfWork.Repository<StaffMealConfig>().GetAllAsync();
            
            foreach (var config in configs)
            {
                var existing = (config.Id > 0) 
                    ? all.FirstOrDefault(x => x.Id == config.Id)
                    : all.FirstOrDefault(x => x.UserId == config.UserId);

                if (existing != null)
                {
                    existing.UserName = config.UserName;
                    existing.ItemId = int.TryParse(config.ItemId, out int iid) ? iid : (int?)null;
                    existing.ItemName = config.ItemName;
                    existing.CategoryId = config.CategoryId;
                    existing.CategoryName = config.CategoryName;
                    existing.GroupId = config.GroupId;
                    existing.GroupName = config.GroupName;
                    existing.DailyLimit = config.DailyLimit;
                    existing.DailyAmountLimit = config.DailyAmountLimit;
                    existing.IsAllItemsAllowed = config.IsAllItemsAllowed;
                    existing.MealLimit = config.MealLimit;
                    existing.SpecialPrice = config.SpecialPrice;
                    existing.IsActive = config.IsActive;
                    _unitOfWork.Repository<StaffMealConfig>().Update(existing);
                }
                else
                {
                    var entity = new StaffMealConfig
                    {
                        UserId = config.UserId!,
                        UserName = config.UserName,
                        ItemId = int.TryParse(config.ItemId, out int iid) ? iid : (int?)null,
                        ItemName = config.ItemName,
                        CategoryId = config.CategoryId,
                        CategoryName = config.CategoryName,
                        GroupId = config.GroupId,
                        GroupName = config.GroupName,
                        DailyLimit = config.DailyLimit,
                        DailyAmountLimit = config.DailyAmountLimit,
                        IsAllItemsAllowed = config.IsAllItemsAllowed,
                        MealLimit = config.MealLimit,
                        SpecialPrice = config.SpecialPrice,
                        IsActive = config.IsActive
                    };
                    await _unitOfWork.Repository<StaffMealConfig>().AddAsync(entity);
                }
            }

            await _unitOfWork.CompleteAsync();
            return true;
        }

        public async Task<IEnumerable<StaffMealGroupDto>> GetAllGroupsAsync()
        {
            var groups = await _unitOfWork.Repository<StaffMealGroup>().GetAllAsync();
            var allUsageItems = await _unitOfWork.Repository<StaffMealGroupItem>().GetAllAsync();
            var allSalesItems = await _unitOfWork.Repository<MenuSalesItems>().GetAllAsync();
            var itemCatMap = allSalesItems.ToDictionary(i => i.Id, i => i.CategoryId ?? 0);

            var dtos = new List<StaffMealGroupDto>();
            foreach (var g in groups)
            {
                var groupItems = allUsageItems.Where(i => i.GroupId == g.Id).ToList();
                
                dtos.Add(new StaffMealGroupDto
                {
                    Id = g.Id,
                    Name = g.Name,
                    ArabicName = g.ArabicName,
                    DailyAmountLimit = g.DailyAmountLimit,
                    IsAllItemsAllowed = g.IsAllItemsAllowed,
                    DailyLimit = g.DailyLimit,
                    MealLimit = g.MealLimit,
                    Items = groupItems.Select(i => {
                        var salesItem = allSalesItems.FirstOrDefault(si => si.Id == i.ItemId);
                        return new StaffMealGroupItemDto
                        {
                            Id = i.Id,
                            GroupId = i.GroupId,
                            ItemId = i.ItemId,
                            ItemName = salesItem?.ArabicName,
                            CategoryId = itemCatMap.TryGetValue(i.ItemId, out var cid) ? cid : 0
                        };
                    }).ToList()
                });
            }
            return dtos;
        }

        public async Task<StaffMealGroupDto?> GetGroupByIdAsync(int groupId)
        {
            var groups = await GetAllGroupsAsync();
            return groups.FirstOrDefault(g => g.Id == groupId);
        }

        public async Task<bool> UpsertGroupAsync(StaffMealGroupDto group)
        {
            if (group.Id > 0)
            {
                var existing = await _unitOfWork.Repository<StaffMealGroup>().GetByIdAsync(group.Id);
                if (existing == null) return false;
                existing.Name = group.Name;
                existing.ArabicName = group.ArabicName;
                existing.DailyAmountLimit = group.DailyAmountLimit;
                existing.IsAllItemsAllowed = group.IsAllItemsAllowed;
                existing.DailyLimit = group.DailyLimit;
                existing.MealLimit = group.MealLimit;
                _unitOfWork.Repository<StaffMealGroup>().Update(existing);
                
                var oldItems = await _unitOfWork.Repository<StaffMealGroupItem>().GetAllAsync();
                foreach(var item in oldItems.Where(i => i.GroupId == group.Id))
                {
                    _unitOfWork.Repository<StaffMealGroupItem>().Delete(item);
                }
            }
            else
            {
                var entity = new StaffMealGroup { Name = group.Name, ArabicName = group.ArabicName, DailyAmountLimit = group.DailyAmountLimit, IsAllItemsAllowed = group.IsAllItemsAllowed };
                await _unitOfWork.Repository<StaffMealGroup>().AddAsync(entity);
                await _unitOfWork.CompleteAsync();
                group.Id = entity.Id;
            }

            foreach (var item in group.Items)
            {
                await _unitOfWork.Repository<StaffMealGroupItem>().AddAsync(new StaffMealGroupItem
                {
                    GroupId = group.Id,
                    ItemId = item.ItemId
                });
            }

            return await _unitOfWork.CompleteAsync() > 0;
        }

        public async Task<bool> DeleteGroupAsync(int groupId)
        {
            var existing = await _unitOfWork.Repository<StaffMealGroup>().GetByIdAsync(groupId);
            if (existing == null) return false;
            _unitOfWork.Repository<StaffMealGroup>().Delete(existing);
            return await _unitOfWork.CompleteAsync() > 0;
        }
    }
}
