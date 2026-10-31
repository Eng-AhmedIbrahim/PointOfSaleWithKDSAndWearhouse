import json

# The 6 items to update
target_names = [
    "CanAccessDelivery",
    "CanAccessTakeAway",
    "CanAccessDistribution",
    "CanAccessAccounts",
    "CanAccessOrders",
    "CanAccessSummary"
]

json_path = r'f:\PointOfSaleProject\src\Pos.Repository\Data\DataSeed\JsonFiles\permissions.json'

with open(json_path, 'r', encoding='utf-8') as f:
    data = json.load(f)

for item in data:
    if item.get("Name") in target_names:
        # Move these to the main POS Nav group as requested
        item["ScreenArabicName"] = "قائمة شاشة المبيعات"
        item["ScreenEnglishName"] = "POS Screen Nav"

with open(json_path, 'w', encoding='utf-8') as f:
    json.dump(data, f, ensure_ascii=False, indent=4)
