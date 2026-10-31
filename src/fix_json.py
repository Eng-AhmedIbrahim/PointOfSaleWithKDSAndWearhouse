import json

def fix_mojibake(text):
    if not isinstance(text, str):
        return text
    try:
        # Try to encode as cp1252 and decode as utf-8
        return text.encode('latin-1').decode('utf-8')
    except:
        try:
             return text.encode('cp1252').decode('utf-8')
        except:
            return text

with open(r'f:\PointOfSaleProject\src\Pos.Repository\Data\DataSeed\JsonFiles\permissions.json', 'r', encoding='utf-8-sig') as f:
    data = json.load(f)

for item in data:
    if 'NameAr' in item:
        item['NameAr'] = fix_mojibake(item['NameAr'])

with open(r'f:\PointOfSaleProject\src\Pos.Repository\Data\DataSeed\JsonFiles\permissions.json', 'w', encoding='utf-8') as f:
    json.dump(data, f, ensure_ascii=False, indent=4)
