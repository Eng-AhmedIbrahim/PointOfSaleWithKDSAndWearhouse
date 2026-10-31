import json

json_path = r'f:\PointOfSaleProject\src\Pos.Repository\Data\DataSeed\JsonFiles\permissions.json'
with open(json_path, 'r', encoding='utf-8') as f:
    data = json.load(f)

# Update the display names for the last BackOffice permissions to avoid confusion
clarifications = {
    "CanViewBackOfficeTransactionsAtBackOffice": { "PoliceArabicName": "عرض قائمة العمليات الرئيسية (إدارة)", "ScreenArabicName": "العمليات (إدارة)" },
    "CanViewBackOfficeReportsAtBackOffice": { "PoliceArabicName": "عرض قائمة التقارير الرئيسية (إدارة)", "ScreenArabicName": "التقارير (إدارة)" },
    "CanViewQueriesAtBackOffice": { "PoliceArabicName": "عرض الاستعلامات (إدارة)", "ScreenArabicName": "الاستعلامات (إدارة)" },
    "CanViewRegistrationAtBackOffice": { "PoliceArabicName": "عرض شاشة التسجيل (إدارة)", "ScreenArabicName": "التسجيل (إدارة)" },
    "CanViewClosingAtBackOffice": { "PoliceArabicName": "عرض شاشة الإغلاق (إدارة)", "ScreenArabicName": "الإغلاق (إدارة)" },
    
    # Let's also clarify the specific Summary/Accounts permissions inside the BackOffice
    "CanViewSummaryDetailsAtBackOffice": { "PoliceArabicName": "عرض المبيعات التفصيلية من الملخص (إدارة)", "ScreenArabicName": "الملخص (إدارة)" },
    "CanPrintSummaryReportAtBackOffice": { "PoliceArabicName": "طباعة يومية المبيعات من الملخص (إدارة)", "ScreenArabicName": "الملخص (إدارة)" },
    "CanPrintStaffAccountsAtBackOffice": { "PoliceArabicName": "طباعة تقارير حسابات الموظفين (إدارة)", "ScreenArabicName": "الحسابات (إدارة)" }
}

changed = 0
for item in data:
    if item["Name"] in clarifications:
        item["PoliceArabicName"] = clarifications[item["Name"]]["PoliceArabicName"]
        item["ScreenArabicName"] = clarifications[item["Name"]]["ScreenArabicName"]
        changed += 1

with open(json_path, 'w', encoding='utf-8') as f:
    json.dump(data, f, ensure_ascii=False, indent=4)

print(f"Updated {changed} permissions.")
