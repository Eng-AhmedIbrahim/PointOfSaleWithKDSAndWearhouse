import json

json_path = r'f:\PointOfSaleProject\src\Pos.Repository\Data\DataSeed\JsonFiles\permissions.json'
with open(json_path, 'r', encoding='utf-8') as f:
    data = json.load(f)

# Sort by group and then by name for a clean UI matrix
data.sort(key=lambda x: (x.get("ScreenEnglishName", ""), x.get("Name", "")))

with open(json_path, 'w', encoding='utf-8') as f:
    json.dump(data, f, ensure_ascii=False, indent=4)
