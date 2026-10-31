# System Update Summary - February 9, 2026

## 1. Permissions System Enhancement

### Added Permissions (All Bilingual - English & Arabic)
All permissions now have both English and Arabic names for proper localization:

- **CanAccessTakeAway** - Take Away Screen (شاشة التيك اواي)
- **CanAccessDistribution** - Distribution Screen (شاشة التوزيع)
- **CanAccessTables** - Tables Screen (شاشة الطاولات)
- **CanAccessDelivery** - Delivery Screen (شاشة التوصيل)
- **CanAccessOrders** - Order Management (إدارة الطلبات)
- **CanAccessKitchen** - Kitchen Display (شاشة المطبخ)
- **CanAccessReport** - Reports Screen (شاشة التقارير)
- **CanAccessSettings** - General Settings (الإعدادات العامة)
- **CanAccessAccounts** - Accounts Screen (شاشة الحسابات)
- **CanAccessSummary** - Daily Summary (الملخص اليومي)
- **CanAccessVoidOrder** - Void Order (إلغاء أوردر)
- **CanAccessTransferTable** - Transfer Table (نقل طاولة)
- **CanAccessMergeTable** - Merge Tables (دمج طاولات)
- **CanAccessSplitOrder** - Split Order (تقسيم أوردر)
- **CanAccessDiscount** - Apply Discount (إضافة خصم)
- **CanAccessPrintReceipt** - Print Receipt (طباعة الفاتورة)
- **CanAccessCloseOrder** - Close Order (إغلاق الأوردر)
- **CanAccessVoidItem** - Void Item (إلغاء صنف)
- **CanAccessUsers** - Users Management (إدارة المستخدمين)
- **CanAccessRoles** - Roles & Permissions (الأدوار والصلاحيات)
- **CanAccessPosSettings** - POS Settings (إعدادات نقاط البيع)
- **CanAccessPrintingSettings** - Printing Settings (إعدادات الطباعة)
- **CanAccessMeals** - Meals Configuration (إعداد الوجبات)
- **CanAccessWaiting** - Waiting List (قائمة الانتظار)

### Role Permissions Mapping

#### Administrator (مدير النظام)
- **Gets ALL permissions automatically**

#### Branch Manager (مدير فرع)
- All main screens (Tables, Delivery, TakeAway, Distribution)
- Order management & reporting
- Settings configuration
- User & role management
- All table operations (Transfer, Merge, Split)
- Discount, Print, Close, Void operations

#### Assistant Manager (مساعد مدير)
- Main screens (Tables, Delivery, TakeAway)
- Order management & reporting
- Table operations (Transfer, Merge, Split)
- Discount, Print, Close, Void operations

#### Cashier (كاشير)
- TakeAway, Delivery, Tables screens
- Order management
- Discount & Print operations
- Waiting list management

#### Hall Captain (كابتن صاله)
- Tables screen
- Order management
- Print receipts
- Table operations (Transfer, Merge)
- Waiting list management

#### Call Center
- Delivery & Distribution screens
- Order management
- Print & Close operations

### Pre-seeded Captains
Two hall captains are automatically created:
1. **CaptainMorning** (كابتن صالة صباحي)
   - Username: CaptainMorning
   - Password: 123456
   - Role: كابتن صاله

2. **CaptainEvening** (كابتن صالة مسائي)
   - Username: CaptainEvening
   - Password: 123456
   - Role: كابتن صاله

## 2. Security Enhancements

### Connection String Encryption
- Connection strings are now encrypted in `appsettings.json`
- Automatic decryption at runtime using AES encryption
- Design-time factories created for EF migrations to work properly
- Files created:
  - `POS.API/Helpers/EncryptionHelper.cs`
  - `Pos.Repository/Factories/AppDbContextFactory.cs`
  - `Pos.Repository/Factories/AppIdentityDbContextFactory.cs`

### UI Authorization
Updated `Section4Buttons.razor` to enforce permissions:
- **Cancel Order** button → requires `CanAccessVoidOrder`
- **Waiting** button → requires `CanAccessWaiting`
- **Print** button → requires `CanAccessPrintReceipt`

## 3. Per-Device Configuration

### Printing Settings
- Each device has its own printing configuration
- Settings are filtered by `ComputerName` (machine name)
- Automatically initialized when a new device connects
- Service: `PrintingSettingsService`

### POS Feature Settings
- Each device can have different feature flags
- Settings are filtered by `ComputerName` (machine name)
- Allows enabling/disabling features per terminal
- Service: `PosFeatureSettingsService`

## 4. Database Migration

Migration `UpdateSettings` created and applied successfully:
- Updated `PosFeatureSettings` table structure
- Updated `PrintingSettings` table structure
- All permissions seeded into database
- Role-permission mappings configured

## Files Modified

### Configuration Files
- `Pos.Repository/Data/DataSeed/JsonFiles/permissions.json`
- `POS.API/appsettings.json`

### Code Files
- `Pos.Repository/Identity/AppIdentityDbContextSeed.cs`
- `POS.API/Program.cs`
- `POS.Desktop/Components/PosComponent/Section4Buttons.razor`

### New Files Created
- `POS.API/Helpers/EncryptionHelper.cs`
- `Pos.Repository/Factories/AppDbContextFactory.cs`
- `Pos.Repository/Factories/AppIdentityDbContextFactory.cs`

## Next Steps for Deployment

1. **Test captain logins** with the pre-seeded accounts
2. **Verify permissions** work correctly in the UI
3. **Test per-device settings** on different machines
4. **Update production appsettings.json** with encrypted connection string
5. **Run database migration** on production: 
   ```bash
   dotnet ef database update --context AppDbContext --project Pos.Repository --startup-project POS.API
   ```

## Security Notes

- Encryption key is hardcoded in `EncryptionHelper.cs` - consider moving to a secure location
- Default captain passwords (123456) should be changed on first login in production
- Connection string encryption provides basic obfuscation but is not military-grade security
- Design-time factories contain plain connection strings for migration purposes only
