import json

# Define new/updated permissions for clarity and detailed control
waiting_actions = [
    { "key": "CanCompleteWaitingOrder", "ar": "تفعيل طلب من الانتظار", "en": "Complete Waiting Order" },
    { "key": "CanRemoveWaitingOrder", "ar": "حذف طلب من الانتظار", "en": "Remove Waiting Order" }
]

summary_actions = [
    { "key": "CanViewSummaryDetails", "ar": "عرض تفاصيل ملخص المبيعات", "en": "View Sales Summary Details" },
    { "key": "CanPrintSummaryReport", "ar": "طباعة ملخص المبيعات", "en": "Print Sales Summary" }
]

all_orders_actions = [
    { "key": "CanViewOrderDetails", "ar": "عرض تفاصيل الطلب", "en": "View Order Details" },
    { "key": "CanPrintOrderReceipt", "ar": "إعادة طباعة فاتورة الطلب", "en": "Reprint Order Receipt" },
    { "key": "CanVoidOrderFromList", "ar": "إلغاء طلب من القائمة", "en": "Void Order from List" }
]

account_actions = [
    { "key": "CanViewStaffAccounts", "ar": "عرض تفاصيل حسابات الموظفين", "en": "View Staff Account Details" },
    { "key": "CanPrintStaffAccounts", "ar": "طباعة تقرير حسابات الموظفين", "en": "Print Staff Account Report" }
]

def update_permissions_files():
    # 1. Update Permissions.cs
    cs_path = r'f:\PointOfSaleProject\src\POS.Authorization\Models\Permissions.cs'
    with open(cs_path, 'r', encoding='utf-8') as f:
        lines = f.readlines()
    
    # Collect all needed keys
    all_keys = []
    for group in [waiting_actions, summary_actions, all_orders_actions, account_actions]:
        for action in group:
            all_keys.append(action["key"])
            
    # Add to C# file if not present
    new_lines = []
    existing_in_cs = "".join(lines)
    
    insertion_point = -1
    for i in range(len(lines)-1, 0, -1):
        if "};" in lines[i]:
            insertion_point = i
            break
            
    if insertion_point != -1:
        for key in all_keys:
            if f'"{key}"' not in existing_in_cs:
                lines.insert(insertion_point, f'        {{ "{key}", "{key}" }},\n')
        
        with open(cs_path, 'w', encoding='utf-8') as f:
            f.writelines(lines)

    # 2. Update permissions.json
    json_path = r'f:\PointOfSaleProject\src\Pos.Repository\Data\DataSeed\JsonFiles\permissions.json'
    with open(json_path, 'r', encoding='utf-8') as f:
        data = json.load(f)
    
    existing_names = [item["Name"] for item in data]
    
    # helper to add or update
    def add_or_update(key, ar, en, screen_ar, screen_en):
        found = False
        for item in data:
            if item["Name"] == key:
                item["PoliceArabicName"] = ar
                item["PoliceEnglishNameEn"] = en
                item["ScreenArabicName"] = screen_ar
                item["ScreenEnglishName"] = screen_en
                found = True
                break
        if not found:
            data.append({
                "Name": key,
                "PoliceArabicName": ar,
                "PoliceEnglishNameEn": en,
                "ScreenArabicName": screen_ar,
                "ScreenEnglishName": screen_en,
                "IsBackOffice": False
            })

    # Add Waiting Actions
    for a in waiting_actions:
        add_or_update(a["key"], a["ar"], a["en"], "شاشة الانتظار", "Waiting Page Actions")
    
    # Add/Update Summary Actions
    for a in summary_actions:
        add_or_update(a["key"], a["ar"], a["en"], "ملخص مبيعات اليوم", "Daily Sales Summary Actions")
    # Also update the main access
    add_or_update("CanAccessSummary", "الدخول لصفحة الملخص", "Access Summary Page", "ملخص مبيعات اليوم", "Daily Sales Summary Actions")

    # Add/Update All Orders Actions
    for a in all_orders_actions:
        add_or_update(a["key"], a["ar"], a["en"], "كل طلبات اليوم", "Today Orders Actions")
    add_or_update("CanAccessOrders", "الدخول لصفحة كل الطلبات", "Access All Orders Page", "كل طلبات اليوم", "Today Orders Actions")

    # Add/Update Accounts Actions
    for a in account_actions:
        add_or_update(a["key"], a["ar"], a["en"], "حسابات الموظفين", "Staff Accounts Actions")
    add_or_update("CanAccessAccounts", "الدخول لصفحة الحسابات", "Access Accounts Page", "حسابات الموظفين", "Staff Accounts Actions")

    with open(json_path, 'w', encoding='utf-8') as f:
        json.dump(data, f, ensure_ascii=False, indent=4)

if __name__ == "__main__":
    update_permissions_files()
