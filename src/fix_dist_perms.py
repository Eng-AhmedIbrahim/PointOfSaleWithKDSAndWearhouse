import json

json_path = r'f:\PointOfSaleProject\src\Pos.Repository\Data\DataSeed\JsonFiles\permissions.json'
with open(json_path, 'r', encoding='utf-8') as f:
    data = json.load(f)

# Descriptive Arabic and English names for each Distribution button
dist_names = {
    "CanAccessDistributionAssignBtn":           ("تعيين سائق للطلب",          "Assign Driver"),
    "CanAccessDistributionCollectBtn":          ("تحصيل مبلغ الطلب",          "Collect Payment"),
    "CanAccessDistributionDriverSettlementBtn": ("تسوية حساب السائق",         "Driver Settlement"),
    "CanAccessDistributionPrintBtn":            ("طباعة طلب التوزيع",         "Print Delivery Order"),
    "CanAccessDistributionUnDispatchBtn":       ("إلغاء إرسال الطلب",         "Un-Dispatch Order"),
    "CanAccessDistributionViewBtn":             ("عرض تفاصيل طلب التوزيع",    "View Dispatch Details"),
    "CanAccessDistributionViewDriversBtn":      ("عرض السائقين",               "View Drivers"),
    "CanAccessDistributionVoidBtn":             ("إلغاء طلب التوزيع",         "Void Dispatch Order"),
    "CanAccessDistributionVoidHistoryBtn":      ("عرض سجل الإلغاءات",         "View Void History"),
}

screen_ar = "عمليات شاشة التوزيع"
screen_en = "Distribution Screen Actions"

for item in data:
    if item["Name"] in dist_names:
        ar_name, en_name = dist_names[item["Name"]]
        item["PoliceArabicName"]   = ar_name
        item["PoliceEnglishNameEn"] = en_name
        item["ScreenArabicName"]   = screen_ar
        item["ScreenEnglishName"]  = screen_en

# Sort
data.sort(key=lambda x: (x.get("ScreenEnglishName", ""), x["Name"]))

with open(json_path, 'w', encoding='utf-8') as f:
    json.dump(data, f, ensure_ascii=False, indent=4)

print("Done. Distribution permissions updated.")
