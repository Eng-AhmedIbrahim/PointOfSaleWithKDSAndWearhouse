import json

json_path = r'f:\PointOfSaleProject\src\Pos.Repository\Data\DataSeed\JsonFiles\permissions.json'
with open(json_path, 'r', encoding='utf-8') as f:
    data = json.load(f)

# Names to remove (old generic ones replaced by the two granular ones)
old_keys_to_remove = {"CanAccessAllOrdersViewBtn", "CanAccessAllOrdersPrintBtn", "CanAccessAllOrdersVoidBtn", "CanPrintOrderReceipt"}

# Remove old ones
data = [item for item in data if item["Name"] not in old_keys_to_remove]

# Check if new ones exist already
existing_names = {item["Name"] for item in data}

new_perms = [
    {
        "Name": "CanViewOrderDetails",
        "PoliceArabicName": "عرض تفاصيل الطلب",
        "PoliceEnglishNameEn": "View Order Details",
        "ScreenArabicName": "كل طلبات اليوم",
        "ScreenEnglishName": "Today Orders Actions",
        "IsBackOffice": False
    },
    {
        "Name": "CanPrintOrderCustomerReceipt",
        "PoliceArabicName": "طباعة فاتورة العميل",
        "PoliceEnglishNameEn": "Print Customer Receipt",
        "ScreenArabicName": "كل طلبات اليوم",
        "ScreenEnglishName": "Today Orders Actions",
        "IsBackOffice": False
    },
    {
        "Name": "CanPrintOrderKitchenReceipt",
        "PoliceArabicName": "طباعة طلبية الكيتشن",
        "PoliceEnglishNameEn": "Print Kitchen Receipt",
        "ScreenArabicName": "كل طلبات اليوم",
        "ScreenEnglishName": "Today Orders Actions",
        "IsBackOffice": False
    },
    {
        "Name": "CanVoidOrderFromList",
        "PoliceArabicName": "إلغاء طلب من القائمة",
        "PoliceEnglishNameEn": "Void Order from List",
        "ScreenArabicName": "كل طلبات اليوم",
        "ScreenEnglishName": "Today Orders Actions",
        "IsBackOffice": False
    }
]

for p in new_perms:
    if p["Name"] not in existing_names:
        data.append(p)
    else:
        # Update in place
        for item in data:
            if item["Name"] == p["Name"]:
                item.update(p)
                break

# Sort
data.sort(key=lambda x: (x["ScreenEnglishName"], x["Name"]))

with open(json_path, 'w', encoding='utf-8') as f:
    json.dump(data, f, ensure_ascii=False, indent=4)

print("Done: Permissions updated successfully.")
