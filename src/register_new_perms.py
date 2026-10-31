import json

# New permissions for Section 3 Actions
new_actions = [
    { "key": "CanUseKeypad", "ar": "استخدام لوحة الأرقام", "en": "Use Keypad" },
    { "key": "CanIncrementQuantity", "ar": "زيادة الكمية (+)", "en": "Increment Quantity" },
    { "key": "CanDecrementQuantity", "ar": "تقليل الكمية (-)", "en": "Decrement Quantity" },
    { "key": "CanDeleteItem", "ar": "حذف صنف من العربة", "en": "Remove Item" },
    { "key": "CanApplyItemDiscount", "ar": "إضافة خصم على صنف", "en": "Item Discount" },
    { "key": "CanEditItemComment", "ar": "تعديل ملاحظات الصنف", "en": "Edit Item Notes" }
]

def update_files():
    # 1. Update Permissions.cs
    cs_path = r'f:\PointOfSaleProject\src\POS.Authorization\Models\Permissions.cs'
    with open(cs_path, 'r', encoding='utf-8') as f:
        lines = f.readlines()
    
    new_lines = []
    found_footer = False
    for line in lines:
        new_lines.append(line)
        if "── POS Screen Footer ──" in line or "── Section 3 Actions ──" in line:
            found_footer = True
            
    if not found_footer:
        # Find a good place to insert, e.g., before the last closing brace
        for i in range(len(lines)-1, 0, -1):
            if "};" in lines[i]:
                insert_pos = i
                for action in new_actions:
                    lines.insert(insert_pos, f'        {{ "{action["key"]}", "{action["key"]}" }},\n')
                lines.insert(insert_pos, '        // ── Section 3 Actions ────────────────────────────────────────────────\n')
                break
        with open(cs_path, 'w', encoding='utf-8') as f:
            f.writelines(lines)

    # 2. Update permissions.json
    json_path = r'f:\PointOfSaleProject\src\Pos.Repository\Data\DataSeed\JsonFiles\permissions.json'
    with open(json_path, 'r', encoding='utf-8') as f:
        data = json.load(f)
    
    existing_names = [item["Name"] for item in data]
    for action in new_actions:
        if action["key"] not in existing_names:
            data.append({
                "Name": action["key"],
                "PoliceArabicName": action["ar"],
                "PoliceEnglishNameEn": action["en"],
                "ScreenArabicName": "أزرار شاشة البيع الجانبية",
                "ScreenEnglishName": "POS Screen Actions",
                "IsBackOffice": False
            })
    
    with open(json_path, 'w', encoding='utf-8') as f:
        json.dump(data, f, ensure_ascii=False, indent=4)

if __name__ == "__main__":
    update_files()
