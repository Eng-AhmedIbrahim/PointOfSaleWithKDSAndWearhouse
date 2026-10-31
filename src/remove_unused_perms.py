import json

json_path = r'f:\PointOfSaleProject\src\Pos.Repository\Data\DataSeed\JsonFiles\permissions.json'
with open(json_path, 'r', encoding='utf-8') as f:
    data = json.load(f)

# Filter out CanAccessPosSettings
data = [p for p in data if p["Name"] != "CanAccessPosSettings"]

with open(json_path, 'w', encoding='utf-8') as f:
    json.dump(data, f, ensure_ascii=False, indent=4)

print("Removed CanAccessPosSettings from permissions.json")
