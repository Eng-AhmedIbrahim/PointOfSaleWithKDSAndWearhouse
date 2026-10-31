import json

cs_path = r'f:\PointOfSaleProject\src\POS.Authorization\Models\Permissions.cs'
json_path = r'f:\PointOfSaleProject\src\Pos.Repository\Data\DataSeed\JsonFiles\permissions.json'

new_perms = [
    {
        "Name": "CanViewDetailedSales",
        "PoliceArabicName": "عرض المبيعات التفصيلية",
        "PoliceEnglishNameEn": "View Detailed Sales",
        "ScreenArabicName": "ملخص مبيعات اليوم",
        "ScreenEnglishName": "Daily Sales Summary Actions",
        "IsBackOffice": False
    },
    {
        "Name": "CanViewSalesItems",
        "PoliceArabicName": "عرض أصناف البيع",
        "PoliceEnglishNameEn": "View Sales Items",
        "ScreenArabicName": "ملخص مبيعات اليوم",
        "ScreenEnglishName": "Daily Sales Summary Actions",
        "IsBackOffice": False
    }
]

# 1. Update Permissions.cs
with open(cs_path, 'r', encoding='utf-8') as f:
    lines = f.readlines()

cs_content = "".join(lines)
for perm in new_perms:
    key = perm["Name"]
    if f'"{key}"' not in cs_content:
        # Find insertion point near Summary Actions
        insertion_i = -1
        for i, line in enumerate(lines):
            if '"CanViewSummaryDetails"' in line:
                insertion_i = i + 1
                break
        if insertion_i != -1:
            lines.insert(insertion_i, f'        {{ "{key}", "{key}" }},\n')

with open(cs_path, 'w', encoding='utf-8') as f:
    f.writelines(lines)

# 2. Update permissions.json
with open(json_path, 'r', encoding='utf-8') as f:
    data = json.load(f)

existing_names = [d["Name"] for d in data]
for perm in new_perms:
    if perm["Name"] not in existing_names:
        data.append(perm)

with open(json_path, 'w', encoding='utf-8') as f:
    json.dump(data, f, ensure_ascii=False, indent=4)

print("Permissions added successfully.")
