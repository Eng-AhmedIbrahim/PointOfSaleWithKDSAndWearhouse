import json

json_path = r'f:\PointOfSaleProject\src\Pos.Repository\Data\DataSeed\JsonFiles\permissions.json'
with open(json_path, 'r', encoding='utf-8') as f:
    data = json.load(f)

# Old names to remove (replaced by semantic names)
obsolete = {
    "CanAccessAccountsViewBtn",
    "CanAccessAccountsPrintBtn",
    "CanAccessSummaryViewDetailsBtn",
    "CanAccessSummaryPrintBtn",
    "CanAccessAllOrdersViewBtn",
    "CanAccessAllOrdersPrintBtn",
    "CanAccessAllOrdersVoidBtn",
    "CanPrintOrderReceipt",
    "CanAccessVoidOrder",
    "CanAccessPrintReceipt",
    "CanAccessPosSettingsFeature"  # will re-add below
}

data = [item for item in data if item["Name"] not in obsolete]
existing = {item["Name"] for item in data}

new_entries = [
    # Waiting Page
    {"Name": "CanCompleteWaitingOrder", "PoliceArabicName": "تفعيل طلب من الانتظار",     "PoliceEnglishNameEn": "Complete Waiting Order",    "ScreenArabicName": "شاشة الانتظار",         "ScreenEnglishName": "Waiting Page Actions",        "IsBackOffice": False},
    {"Name": "CanRemoveWaitingOrder",   "PoliceArabicName": "حذف طلب من الانتظار",       "PoliceEnglishNameEn": "Remove Waiting Order",      "ScreenArabicName": "شاشة الانتظار",         "ScreenEnglishName": "Waiting Page Actions",        "IsBackOffice": False},
    # Summary
    {"Name": "CanViewSummaryDetails",   "PoliceArabicName": "عرض تفاصيل ملخص المبيعات", "PoliceEnglishNameEn": "View Sales Summary Details","ScreenArabicName": "ملخص مبيعات اليوم",    "ScreenEnglishName": "Daily Sales Summary Actions", "IsBackOffice": False},
    {"Name": "CanPrintSummaryReport",   "PoliceArabicName": "طباعة ملخص المبيعات",       "PoliceEnglishNameEn": "Print Sales Summary",       "ScreenArabicName": "ملخص مبيعات اليوم",    "ScreenEnglishName": "Daily Sales Summary Actions", "IsBackOffice": False},
    # Accounts
    {"Name": "CanViewStaffAccounts",    "PoliceArabicName": "عرض تفاصيل حسابات الموظفين","PoliceEnglishNameEn": "View Staff Account Details","ScreenArabicName": "حسابات الموظفين",      "ScreenEnglishName": "Staff Accounts Actions",      "IsBackOffice": False},
    {"Name": "CanPrintStaffAccounts",   "PoliceArabicName": "طباعة تقرير حسابات الموظفين","PoliceEnglishNameEn":"Print Staff Account Report","ScreenArabicName": "حسابات الموظفين",      "ScreenEnglishName": "Staff Accounts Actions",      "IsBackOffice": False},
    # All Orders (Today)
    {"Name": "CanViewOrderDetails",           "PoliceArabicName": "عرض تفاصيل الطلب",        "PoliceEnglishNameEn": "View Order Details",           "ScreenArabicName": "كل طلبات اليوم", "ScreenEnglishName": "Today Orders Actions", "IsBackOffice": False},
    {"Name": "CanPrintOrderCustomerReceipt",  "PoliceArabicName": "طباعة فاتورة العميل",      "PoliceEnglishNameEn": "Print Customer Receipt",       "ScreenArabicName": "كل طلبات اليوم", "ScreenEnglishName": "Today Orders Actions", "IsBackOffice": False},
    {"Name": "CanPrintOrderKitchenReceipt",   "PoliceArabicName": "طباعة طلبية الكيتشن",     "PoliceEnglishNameEn": "Print Kitchen Receipt",        "ScreenArabicName": "كل طلبات اليوم", "ScreenEnglishName": "Today Orders Actions", "IsBackOffice": False},
    {"Name": "CanVoidOrderFromList",          "PoliceArabicName": "إلغاء طلب من القائمة",     "PoliceEnglishNameEn": "Void Order from List",         "ScreenArabicName": "كل طلبات اليوم", "ScreenEnglishName": "Today Orders Actions", "IsBackOffice": False},
    # Global
    {"Name": "CanAccessPosSettingsFeature",   "PoliceArabicName": "إعدادات النظام المتقدمة",  "PoliceEnglishNameEn": "POS Advanced Settings Feature","ScreenArabicName": "الإعدادات العامة","ScreenEnglishName": "Global Settings",     "IsBackOffice": False},
]

for entry in new_entries:
    if entry["Name"] not in existing:
        data.append(entry)
    else:
        for item in data:
            if item["Name"] == entry["Name"]:
                item.update(entry)
                break

# Sort
data.sort(key=lambda x: (x.get("ScreenEnglishName",""), x["Name"]))

with open(json_path, 'w', encoding='utf-8') as f:
    json.dump(data, f, ensure_ascii=False, indent=4)

print(f"Done. Total permissions in JSON: {len(data)}")
