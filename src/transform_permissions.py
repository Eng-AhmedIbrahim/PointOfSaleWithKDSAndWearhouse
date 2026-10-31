import json

def get_mapping(name, is_back_office):
    # Default Categories
    tag_ar, tag_en = "عمليات أخرى", "Other Operations"
    friendly_ar, friendly_en = name, name

    # Category Mapping
    if name.startswith("CanAccessDelivery"):
        tag_en, tag_ar = "Delivery Screen", "شاشة التوصيل"
    elif name.startswith("CanAccessDineIn"):
        tag_en, tag_ar = "Dine-In Screen", "شاشة الصالة"
    elif name.startswith("CanAccessTakeAway") or name.startswith("CanAccessTables"):
        tag_en, tag_ar = "Sales Screens", "شاشات المبيعات"
    elif name.startswith("CanAccessFooter"):
        tag_en, tag_ar = "POS Screen Footer", "أزرار اسفل شاشة البيع"
    elif name.startswith("CanAccessDistribution"):
        tag_en, tag_ar = "Distribution", "التوزيع"
    elif name.startswith("CanAccessAllOrders") or name.startswith("CanAccessOrders"):
        tag_en, tag_ar = "Orders Management", "إدارة الطلبات"
    elif any(x in name for x in ["User", "Role"]):
        tag_en, tag_ar = "Security & Users", "المستخدمين والمشرفين"
    elif any(x in name for x in ["Settings", "Printer", "Printing"]):
        tag_en, tag_ar = "Settings", "الإعدادات"
    elif "Account" in name:
        tag_en, tag_ar = "Accounts", "الحسابات"
    elif "Summary" in name or "Report" in name:
        tag_en, tag_ar = "Reports & Summary", "التقارير والملخصات"
    elif is_back_office:
        tag_en, tag_ar = "BackOffice Nav", "قائمة الإدارة العلوية"
    else:
        tag_en, tag_ar = "POS Screen Nav", "قائمة شاشة المبيعات"

    # Friendly Names Mapping
    names_map = {
        # Dine-In
        "CanAccessDineInOrderBtn": ("طلب صالة", "Dine-In Order"),
        "CanAccessDineInReceiptBtn": ("طباعة فواتير الصالة", "Dine-In Receipt"),
        "CanAccessDineInCloseTableBtn": ("غلق الطاولة", "Close Table"),
        "CanAccessDineInSplitOrderBtn": ("تقسيم الطلب", "Split Order"),
        "CanAccessDineInMergeTableBtn": ("دمج الطاولات", "Merge Tables"),
        "CanAccessDineInTransferBtn": ("تحويل الطاولة", "Transfer Table"),
        "CanAccessDineInVoidBtn": ("إلغاء طلب صالة", "Void Dine-In"),
        "CanAccessDineInGuestCountBtn": ("عدد الضيوف", "Guest Count"),
        
        # Delivery
        "CanAccessDeliveryOrderBtn": ("طلب توصيل", "Delivery Order"),
        "CanAccessDeliveryAddNewBtn": ("إضافة عميل جديد", "Add New Customer"),
        "CanAccessDeliveryClearBtn": ("مسح البيانات", "Clear Data"),
        "CanAccessDeliveryComplaintsBtn": ("الشكاوى", "Complaints"),
        "CanAccessDeliverySearchBtn": ("بحث توصيل", "Delivery Search"),
        "CanAccessDeliveryBranchManagementBtn": ("إدارة الفروع", "Branch Management"),
        "CanAccessDeliveryDistributionBtn": ("التوزيع", "Distribution"),
        "CanAccessDeliveryToggleDirectionBtn": ("تغيير الاتجاه", "Toggle Direction"),
        
        # Screens & Global
        "CanAccessTables": ("شاشة الطاولات", "Tables Screen"),
        "CanAccessDelivery": ("شاشة التوصيل", "Delivery Screen"),
        "CanAccessTakeAway": ("شاشة التيك اواي", "Take-Away Screen"),
        "CanAccessAccounts": ("الحسابات", "Accounts"),
        "CanAccessSummary": ("الملخص اليومي", "Daily Summary"),
        "CanAccessOrders": ("إدارة الطلبات", "Orders Management"),
        "CanAccessDistribution": ("التوزيع", "Distribution"),
        "CanAccessDiscount": ("الخصومات", "Discounts"),
        "CanAccessMeals": ("الوجبات", "Meals"),
        "CanAccessWaiting": ("قائمة الانتظار", "Waiting List"),
        "CanAccessSettings": ("إعدادات النظام", "System Settings"),
        "CanAccessKitchen": ("دخول المطبخ", "Kitchen Access"),
        "CanAccessReport": ("التقارير", "Reports"),
        "CanAccessVoidOrder": ("إلغاء فاتورة كاملة", "Void Full Order"),
        "CanAccessVoidItem": ("إلغاء صنف", "Void Item"),
        "CanAccessUsers": ("إدارة المستخدمين", "User Management"),
        "CanAccessRoles": ("إدارة الأدوار", "Role Management"),
        "CanAccessPosSettings": ("إعدادات الكاشير", "POS Settings"),
        "CanAccessPrintingSettings": ("إعدادات الطباعة", "Printing Settings"),
        "CanAccessFooterDiscountBtn": ("زر الخصم (Footer)", "Discount Button"),
        "CanAccessFooterCustomerDataBtn": ("بيانات العميل (Footer)", "Customer Data Button"),
        "CanAccessFooterPaymentMethodBtn": ("طرق الدفع (Footer)", "Payment Method Button"),
        "CanAccessFooterQuickPaymentBtn": ("دفع سريع (Footer)", "Quick Pay Button"),
        "CanAccessFooterMealsBtn": ("زر الوجبات (Footer)", "Meals Button"),
        "CanAccessFooterWaitingBtn": ("الانتظار (Footer)", "Waiting Button"),
        "CanAccessFooterSettingsBtn": ("الإعدادات (Footer)", "Settings Button"),
    }

    if name in names_map:
        friendly_ar, friendly_en = names_map[name]
    
    return tag_ar, tag_en, friendly_ar, friendly_en

with open(r'f:\PointOfSaleProject\src\Pos.Repository\Data\DataSeed\JsonFiles\permissions.json', 'r', encoding='utf-8') as f:
    data = json.load(f)

new_data = []
for item in data:
    name = item.get("Name", "")
    if not name: continue
    
    is_back = item.get("IsBackOffice", False)
    tag_ar, tag_en, friendly_ar, friendly_en = get_mapping(name, is_back)
    
    new_item = {
        "Name": name,
        "PoliceArabicName": friendly_ar,
        "PoliceEnglishNameEn": friendly_en,
        "ScreenArabicName": tag_ar,
        "ScreenEnglishName": tag_en,
        "IsBackOffice": is_back
    }
    new_data.append(new_item)

with open(r'f:\PointOfSaleProject\src\Pos.Repository\Data\DataSeed\JsonFiles\permissions.json', 'w', encoding='utf-8') as f:
    json.dump(new_data, f, ensure_ascii=False, indent=4)
