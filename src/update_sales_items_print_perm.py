import json

cs_path = r'f:\PointOfSaleProject\src\POS.Authorization\Models\Permissions.cs'
json_path = r'f:\PointOfSaleProject\src\Pos.Repository\Data\DataSeed\JsonFiles\permissions.json'
seed_path = r'f:\PointOfSaleProject\src\Pos.Repository\Identity\AppIdentityDbContextSeed.cs'

new_perm = {
    "Name": "CanPrintSalesItems",
    "PoliceArabicName": "طباعة أصناف البيع",
    "PoliceEnglishNameEn": "Print Sales Items",
    "ScreenArabicName": "ملخص مبيعات اليوم",
    "ScreenEnglishName": "Daily Sales Summary Actions",
    "IsBackOffice": False
}

# 1. Update Permissions.cs
with open(cs_path, 'r', encoding='utf-8') as f:
    lines = f.readlines()

cs_content = "".join(lines)
key = new_perm["Name"]
if f'"{key}"' not in cs_content:
    insertion_i = -1
    for i, line in enumerate(lines):
        if '"CanViewSalesItems"' in line:
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
if key not in existing_names:
    data.append(new_perm)

with open(json_path, 'w', encoding='utf-8') as f:
    json.dump(data, f, ensure_ascii=False, indent=4)

# 3. Update AppIdentityDbContextSeed.cs
target = '"CanViewDetailedSales", "CanViewSalesItems", "CanPrintSummaryReport",'
replacement = '"CanViewDetailedSales", "CanViewSalesItems", "CanPrintSalesItems", "CanPrintSummaryReport",'

with open(seed_path, "r", encoding="utf-8") as f:
    content = f.read()

if target in content and key not in content:
    content = content.replace(target, replacement)
    with open(seed_path, "w", encoding="utf-8") as f:
        f.write(content)
    print("Seed updated.")

print("Process completed.")
