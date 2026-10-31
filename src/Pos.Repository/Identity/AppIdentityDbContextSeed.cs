namespace Pos.Repository.Identity;

public static class AppIdentityDbContextSeed
{
    private static readonly List<string> potentialFilePaths =
    [
        Path.Combine("Data", "DataSeed","JsonFiles"),
        Path.Combine("..", "Pos.Repository", "Data", "DataSeed","JsonFiles"),
        Path.Combine("f:", "PointOfSaleProject", "src", "Pos.Repository", "Data", "DataSeed","JsonFiles"),
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "DataSeed","JsonFiles"),
    ];

    public static async Task SeedAsync(UserManager<AppUser> userManager, RoleManager<ApplicationRole> roleManager, AppIdentityDbContext context)
    {

        if (!context.Roles.Any())
        {
            var roles = (await GetDataFromJsonFile<ApplicationRole>("roles.json")).ToList();
            foreach (var role in roles)
            {
                await roleManager.CreateAsync(role);
            }
        }

        var allDefinedPermissions = await GetDataFromJsonFile<Permission>("permissions.json");
        foreach (var perm in allDefinedPermissions)
        {
            var existing = await context.Permissions.FirstOrDefaultAsync(p => p.Name == perm.Name);
            if (existing == null)
            {
                perm.Id = 0; // Fixes SQL identity insert error
                await context.Permissions.AddAsync(perm);
            }
            else
            {
                // Update properties if they changed
                existing.PoliceArabicName = perm.PoliceArabicName;
                existing.PoliceEnglishNameEn = perm.PoliceEnglishNameEn;
                existing.ScreenArabicName = perm.ScreenArabicName;
                existing.ScreenEnglishName = perm.ScreenEnglishName;
                existing.IsBackOffice = perm.IsBackOffice;
                existing.IsWarehouse = perm.IsWarehouse;
                context.Permissions.Update(existing);
            }
        }
        await context.SaveChangesAsync();

        if (!await userManager.Users.AnyAsync())
        {
            var user = new AppUser()
            {
                Email = "Administrator",
                UserName = "Administrator",
                DisplayName = "Administrator",
                ArabicName = "Administrator",
                RegistrationDate = DateTime.Now,
                EmailConfirmed = true,
                IsActive = true
            };

            var result = await userManager.CreateAsync(user, "951753");

            if (result.Succeeded)
            {
                var roleResult = await userManager.AddToRoleAsync(user, "Administrator");

                if (!roleResult.Succeeded)
                {
                    var roleErrors = string.Join(", ", roleResult.Errors.Select(e => e.Description));
                    throw new Exception($"Role assignment failed: {roleErrors}");
                }
            }
            else
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new Exception($"User creation failed: {errors}");
            }
        }

        if (!await userManager.Users.AnyAsync(u => u.UserName == "CaptainMorning"))
        {
            var user = new AppUser()
            {
                Email = "CaptainMorning@pos.com",
                UserName = "CaptainMorning",
                RegistrationDate = DateTime.Now,
                EmailConfirmed = true,
                DisplayName = "كابتن صالة صباحي",
                ArabicName = "كابتن صالة صباحي",
                IsActive = true
            };
            var result = await userManager.CreateAsync(user, "123456");
            if (result.Succeeded)
            {
                var role = await roleManager.FindByIdAsync("5");
                if (role != null && !string.IsNullOrEmpty(role.Name))
                {
                    await userManager.AddToRoleAsync(user, role.Name);
                }
            }
        }

        if (!await userManager.Users.AnyAsync(u => u.UserName == "CaptainEvening"))
        {
            var user = new AppUser()
            {
                Email = "CaptainEvening@pos.com",
                UserName = "CaptainEvening",
                RegistrationDate = DateTime.Now,
                EmailConfirmed = true,
                DisplayName = "كابتن صالة مسائي",
                ArabicName = "كابتن صالة مسائي",
                IsActive = true
            };
            var result = await userManager.CreateAsync(user, "123456");
            if (result.Succeeded)
            {
                var role = await roleManager.FindByIdAsync("5");
                if (role != null && !string.IsNullOrEmpty(role.Name))
                {
                    await userManager.AddToRoleAsync(user, role.Name);
                }
            }
        }

        if (!await userManager.Users.AnyAsync(u => u.UserName == "DriverMorning"))
        {
            var user = new AppUser()
            {
                Email = "DriverMorning@pos.com",
                UserName = "DriverMorning",
                RegistrationDate = DateTime.Now,
                EmailConfirmed = true,
                DisplayName = "طيار صباحي",
                ArabicName = "طيار صباحي",
                IsActive = true
            };
            var result = await userManager.CreateAsync(user, "123456");
            if (result.Succeeded)
            {
                var role = await roleManager.FindByIdAsync("17");
                if (role != null && !string.IsNullOrEmpty(role.Name))
                    await userManager.AddToRoleAsync(user, role.Name);
            }
        }

        if (!await userManager.Users.AnyAsync(u => u.UserName == "DriverEvening"))
        {
            var user = new AppUser()
            {
                Email = "DriverEvening@pos.com",
                UserName = "DriverEvening",
                RegistrationDate = DateTime.Now,
                EmailConfirmed = true,
                DisplayName = "طيار مسائي",
                ArabicName = "طيار مسائي",
                IsActive = true
            };
            var result = await userManager.CreateAsync(user, "123456");
            if (result.Succeeded)
            {
                var role = await roleManager.FindByIdAsync("17");
                if (role != null && !string.IsNullOrEmpty(role.Name))
                    await userManager.AddToRoleAsync(user, role.Name);
            }
        }

        if (!await userManager.Users.AnyAsync(u => u.UserName == "CashierMorning"))
        {
            var user = new AppUser()
            {
                Email = "CashierMorning@pos.com",
                UserName = "CashierMorning",
                RegistrationDate = DateTime.Now,
                EmailConfirmed = true,
                DisplayName = "كاشير صباحي",
                ArabicName = "كاشير صباحي",
                IsActive = true
            };
            var result = await userManager.CreateAsync(user, "123456");
            if (result.Succeeded)
            {
                var role = await roleManager.FindByIdAsync("4");
                if (role != null && !string.IsNullOrEmpty(role.Name))
                    await userManager.AddToRoleAsync(user, role.Name);
            }
        }

        if (!await userManager.Users.AnyAsync(u => u.UserName == "CashierEvening"))
        {
            var user = new AppUser()
            {
                Email = "CashierEvening@pos.com",
                UserName = "CashierEvening",
                RegistrationDate = DateTime.Now,
                EmailConfirmed = true,
                DisplayName = "كاشير مسائي",
                ArabicName = "كاشير مسائي",
                IsActive = true
            };
            var result = await userManager.CreateAsync(user, "123456");
            if (result.Succeeded)
            {
                var role = await roleManager.FindByIdAsync("4");
                if (role != null && !string.IsNullOrEmpty(role.Name))
                    await userManager.AddToRoleAsync(user, role.Name);
            }
        }

        await SeedRoleClaims(roleManager, context);
    }


    private static string FindValidFilePath(List<string> paths, string fileName)
    {
        foreach (var path in paths)
        {
            var fullPath = Path.Combine(path, fileName);
            if (File.Exists(fullPath))
                return fullPath;
        }
        return string.Empty;
    }

    public static async Task<List<T>> GetDataFromJsonFile<T>(string fileName)
    {
        var filePath = FindValidFilePath(potentialFilePaths, fileName);
        if (string.IsNullOrEmpty(filePath))
            return [];

        var data = await File.ReadAllTextAsync(filePath);
        var options = new JsonSerializerOptions { AllowTrailingCommas = true, PropertyNameCaseInsensitive = true };
        return JsonSerializer.Deserialize<List<T>>(data, options) ?? [];
    }


    public static async Task SeedRoleClaims(RoleManager<ApplicationRole> roleManager, AppIdentityDbContext context)
    {
        var allPermissions = await context.Permissions.Select(p => p.Name).ToListAsync();

        // ─── Administrator: gets ALL permissions automatically ─────────────────
        var adminRole = await roleManager.FindByNameAsync("Administrator");
        if (adminRole != null)
        {
            var existingClaims = await roleManager.GetClaimsAsync(adminRole);
            foreach (var permission in allPermissions)
            {
                if (!existingClaims.Any(c => c.Type == "Permission" && c.Value == permission))
                    await roleManager.AddClaimAsync(adminRole, new Claim("Permission", permission));
            }
        }

        // ─── Other roles ───────────────────────────────────────────────────────
        var rolePermissions = new Dictionary<string, List<string>>
        {
            // ══════════════════════════════════════════════════════════════════
            // مدير فرع  –  Full POS access, all screens, full reporting
            // ══════════════════════════════════════════════════════════════════
            { "مدير فرع", new List<string>
            {
                // Nav / Screens
                "CanAccessTables", "CanAccessDelivery", "CanAccessTakeAway",
                "CanAccessDistribution", "CanAccessOrders", "CanAccessSummary",
                "CanAccessAccounts", "CanAccessWaiting",
                // Section 3 – Item Actions
                "CanUseKeypad", "CanIncrementQuantity", "CanDecrementQuantity",
                "CanDeleteItem", "CanApplyItemDiscount", "CanEditItemComment",

                // Section 4 – Order Actions
                "CanPrintOrder", "CanWaitingOrder", "CanCancelOrder",

                // Footer Buttons
                "CanAccessFooterDiscountBtn", "CanAccessFooterCustomerDataBtn",
                "CanAccessFooterPaymentMethodBtn", "CanAccessFooterQuickPaymentBtn",
                "CanAccessFooterMealsBtn", "CanAccessFooterWaitingBtn", "CanAccessFooterSettingsBtn",

                // DineIn Buttons
                "CanAccessDineInOrderBtn", "CanAccessDineInReceiptBtn", "CanAccessDineInCloseTableBtn",
                "CanAccessDineInSplitOrderBtn", "CanAccessDineInMergeTableBtn",
                "CanAccessDineInTransferBtn", "CanAccessDineInVoidBtn", "CanAccessDineInGuestCountBtn",

                // Delivery Buttons
                "CanAccessDeliveryOrderBtn", "CanAccessDeliveryAddNewBtn", "CanAccessDeliveryClearBtn",
                "CanAccessDeliveryComplaintsBtn", "CanAccessDeliverySearchBtn",
                "CanAccessDeliveryBranchManagementBtn", "CanAccessDeliveryDistributionBtn",
                "CanAccessDeliveryToggleDirectionBtn", "CanAccessDeliveryVoidBtn", "CanAccessDeliveryEditBtn",

                // All Orders (Today)
                "CanViewOrderDetails", "CanPrintOrderCustomerReceipt",
                "CanPrintOrderKitchenReceipt", "CanVoidOrderFromList",

                // Distribution
                "CanAccessDistributionAssignBtn", "CanAccessDistributionViewBtn",
                "CanAccessDistributionVoidBtn", "CanAccessDistributionPrintBtn",
                "CanAccessDistributionUnDispatchBtn", "CanAccessDistributionCollectBtn",
                "CanAccessDistributionVoidHistoryBtn", "CanAccessDistributionDriverSettlementBtn",
                "CanAccessDistributionViewDriversBtn",

                // Waiting Page Actions
                "CanCompleteWaitingOrder", "CanRemoveWaitingOrder",

                // Summary Actions
                "CanViewSummaryDetails", "CanViewDetailedSales", "CanViewSalesItems", "CanPrintSalesItems", "CanPrintSummaryReport",

                // Accounts Actions
                "CanViewStaffAccounts", "CanPrintStaffAccounts",

                // Global Feature Flags
                "CanAccessPosSettingsFeature",

                // Back Office Permissions
                "CanAccessBackOffice", "CanViewDashboardAtBackOffice", "CanManageCompanySettingsAtBackOffice",
                "CanManageOrderSettingsAtBackOffice", "CanManagePrinterSettingsAtBackOffice", "CanManageSystemFeaturesAtBackOffice",
                "CanSyncDataAtBackOffice", "CanManageUsersAtBackOffice", "CanManageRolesAtBackOffice",
                "CanManagePermissionsAtBackOffice", "CanManageItemsAtBackOffice", "CanManageCategoriesAtBackOffice",
                "CanManageTablesAtBackOffice", "CanManageZonesAtBackOffice", "CanManageInventoryAtBackOffice",
                "CanAssignDriverAtBackOffice", "CanViewOrderAtBackOffice", "CanVoidOrderAtBackOffice",
                "CanPrintOrderAtBackOffice", "CanPrintKitchenOrderAtBackOffice", "CanUnDispatchOrderAtBackOffice",
                "CanCollectOrderAtBackOffice", "CanViewDriversListAtBackOffice", "CanViewVoidHistoryAtBackOffice",
                "CanViewDriverSettlementAtBackOffice", "CanViewSummaryDetailsAtBackOffice", 
                "CanPrintSummaryReportAtBackOffice", "CanPrintStaffAccountsAtBackOffice",
                "CanViewBackOfficeTransactionsAtBackOffice", "CanViewBackOfficeReportsAtBackOffice",
                "CanManageGeneralSettingsAtBackOffice", "CanManagePosSettingsAtBackOffice", "CanManageLanguageSettingsAtBackOffice",
                "CanManagePaymentMethodsAtBackOffice", "CanManageRawMaterialsAtBackOffice", "CanManageSemiFinishedItemsAtBackOffice",
                "CanManageRecipesAtBackOffice", "CanManageUnitsAtBackOffice", "CanViewQueriesAtBackOffice",
                "CanViewRegistrationAtBackOffice", "CanViewClosingAtBackOffice"
            } },

            // ══════════════════════════════════════════════════════════════════
            // مساعد مدير  –  Most POS access, no void, no branch mgmt
            // ══════════════════════════════════════════════════════════════════
            { "مساعد مدير", new List<string>
            {
                // Nav / Screens
                "CanAccessTables", "CanAccessDelivery", "CanAccessTakeAway",
                "CanAccessOrders", "CanAccessSummary", "CanAccessAccounts",
                "CanAccessWaiting", "CanAccessDistribution",

                // Section 3 – Item Actions
                "CanUseKeypad", "CanIncrementQuantity", "CanDecrementQuantity",
                "CanDeleteItem", "CanApplyItemDiscount", "CanEditItemComment",

                // Section 4 – Order Actions
                "CanPrintOrder", "CanWaitingOrder", "CanCancelOrder",

                // Footer Buttons
                "CanAccessFooterDiscountBtn", "CanAccessFooterCustomerDataBtn",
                "CanAccessFooterPaymentMethodBtn", "CanAccessFooterQuickPaymentBtn",
                "CanAccessFooterWaitingBtn", "CanAccessFooterSettingsBtn",

                // DineIn Buttons
                "CanAccessDineInOrderBtn", "CanAccessDineInReceiptBtn", "CanAccessDineInCloseTableBtn",
                "CanAccessDineInSplitOrderBtn", "CanAccessDineInMergeTableBtn",
                "CanAccessDineInTransferBtn", "CanAccessDineInVoidBtn", "CanAccessDineInGuestCountBtn",

                // Delivery Buttons
                "CanAccessDeliveryOrderBtn", "CanAccessDeliveryAddNewBtn", "CanAccessDeliveryClearBtn",
                "CanAccessDeliveryComplaintsBtn", "CanAccessDeliverySearchBtn",
                "CanAccessDeliveryToggleDirectionBtn", "CanAccessDeliveryVoidBtn", "CanAccessDeliveryEditBtn",

                // All Orders (Today)
                "CanViewOrderDetails", "CanPrintOrderCustomerReceipt", "CanPrintOrderKitchenReceipt",

                // Distribution (limited – no void/undispatch)
                "CanAccessDistributionViewBtn", "CanAccessDistributionPrintBtn",
                "CanAccessDistributionCollectBtn", "CanAccessDistributionViewDriversBtn",

                // Waiting Page Actions
                "CanCompleteWaitingOrder", "CanRemoveWaitingOrder",

                // Summary Actions
                "CanViewSummaryDetails", "CanViewDetailedSales", "CanViewSalesItems", "CanPrintSalesItems", "CanPrintSummaryReport",

                // Accounts Actions
                "CanViewStaffAccounts", "CanPrintStaffAccounts"
            } },

            // ══════════════════════════════════════════════════════════════════
            // كاشير  –  Basic POS: take order, print, waiting; no void/cancel
            // ══════════════════════════════════════════════════════════════════
            { "كاشير", new List<string>
            {
                // Nav / Screens
                "CanAccessTakeAway", "CanAccessDelivery", "CanAccessTables",
                "CanAccessOrders", "CanAccessWaiting",

                // Section 3 – Item Actions
                "CanUseKeypad", "CanIncrementQuantity", "CanDecrementQuantity",

                // Section 4 – Order Actions
                "CanPrintOrder", "CanWaitingOrder",

                // Footer Buttons
                "CanAccessFooterCustomerDataBtn", "CanAccessFooterPaymentMethodBtn",
                "CanAccessFooterQuickPaymentBtn", "CanAccessFooterWaitingBtn",
                "CanAccessFooterMealsBtn",

                // DineIn Buttons
                "CanAccessDineInOrderBtn", "CanAccessDineInReceiptBtn",
                "CanAccessDineInCloseTableBtn", "CanAccessDineInGuestCountBtn",

                // Delivery Buttons
                "CanAccessDeliveryOrderBtn", "CanAccessDeliveryAddNewBtn",
                "CanAccessDeliveryClearBtn", "CanAccessDeliverySearchBtn",
                "CanAccessDeliveryToggleDirectionBtn", "CanAccessDeliveryVoidBtn", "CanAccessDeliveryEditBtn",

                // All Orders (Today) – view & print customer only
                "CanViewOrderDetails", "CanPrintOrderCustomerReceipt",

                // Waiting Page
                "CanCompleteWaitingOrder"
            } },

            // ══════════════════════════════════════════════════════════════════
            // كابتن صاله  –  DineIn focused: order, receipt, table mgmt
            // ══════════════════════════════════════════════════════════════════
            { "كابتن صاله", new List<string>
            {
                // Nav / Screens
                "CanAccessTables", "CanAccessOrders", "CanAccessWaiting",

                // Section 3 – Item Actions
                "CanUseKeypad", "CanIncrementQuantity", "CanDecrementQuantity",
                "CanEditItemComment",

                // Section 4 – Order Actions
                "CanPrintOrder", "CanWaitingOrder",

                // Footer Buttons
                "CanAccessFooterCustomerDataBtn", "CanAccessFooterWaitingBtn",
                "CanAccessFooterMealsBtn",

                // DineIn Buttons
                "CanAccessDineInOrderBtn", "CanAccessDineInReceiptBtn",
                "CanAccessDineInCloseTableBtn", "CanAccessDineInTransferBtn",
                "CanAccessDineInMergeTableBtn", "CanAccessDineInGuestCountBtn",

                // All Orders (Today) – view only
                "CanViewOrderDetails",

                // Waiting Page
                "CanCompleteWaitingOrder"
            } },

            // ══════════════════════════════════════════════════════════════════
            // Call Center  –  Delivery & distribution only
            // ══════════════════════════════════════════════════════════════════
            { "Call Center", new List<string>
            {
                // Nav / Screens
                "CanAccessDelivery", "CanAccessDistribution", "CanAccessOrders",

                // Footer Buttons
                "CanAccessFooterCustomerDataBtn", "CanAccessFooterPaymentMethodBtn",

                // Delivery Buttons – full access
                "CanAccessDeliveryOrderBtn", "CanAccessDeliveryAddNewBtn",
                "CanAccessDeliveryClearBtn", "CanAccessDeliveryComplaintsBtn",
                "CanAccessDeliverySearchBtn", "CanAccessDeliveryBranchManagementBtn",
                "CanAccessDeliveryDistributionBtn", "CanAccessDeliveryToggleDirectionBtn", "CanAccessDeliveryVoidBtn", "CanAccessDeliveryEditBtn",

                // All Orders (Today) – view only
                "CanViewOrderDetails",

                // Distribution – full operational access
                "CanAccessDistributionAssignBtn", "CanAccessDistributionViewBtn",
                "CanAccessDistributionVoidBtn", "CanAccessDistributionPrintBtn",
                "CanAccessDistributionUnDispatchBtn", "CanAccessDistributionCollectBtn",
                "CanAccessDistributionVoidHistoryBtn", "CanAccessDistributionViewDriversBtn"
            } },

            // ══════════════════════════════════════════════════════════════════
            // طيار  –  Distribution & Order view only
            // ══════════════════════════════════════════════════════════════════
            { "طيار", new List<string>
            {
                "CanAccessDistribution",
                "CanAccessDistributionViewBtn",
                "CanAccessDistributionPrintBtn",
                "CanAccessOrders",
                "CanViewOrderDetails"
            } }
        };

        foreach (var roleEntry in rolePermissions)
        {
            var identityRole = await roleManager.FindByNameAsync(roleEntry.Key);
            if (identityRole == null) continue;

            var existingClaims = await roleManager.GetClaimsAsync(identityRole);
            foreach (var permission in roleEntry.Value)
            {
                // Only add if it actually exists in the DB (skip stale permission names)
                if (allPermissions.Contains(permission) &&
                    !existingClaims.Any(c => c.Type == "Permission" && c.Value == permission))
                {
                    await roleManager.AddClaimAsync(identityRole, new Claim("Permission", permission));
                }
            }
        }
    }

}

