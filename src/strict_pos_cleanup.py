import json

# Golden Set of Permissions for POS.Desktop (after re-evaluation)
golden_set = {
    # Nav / Screens
    "CanAccessTables": "Tables Screen",
    "CanAccessDelivery": "Delivery Screen",
    "CanAccessTakeAway": "Take-Away Screen",
    "CanAccessDistribution": "Distribution Screen",
    "CanAccessWaiting": "Waiting Screen",
    "CanAccessOrders": "Orders List",
    "CanAccessAccounts": "Accounts Screen",
    "CanAccessSummary": "Summary Screen",
    "CanAccessPosSettings": "POS Settings",

    # Section 3 (Actions)
    "CanUseKeypad": "Use Keypad",
    "CanIncrementQuantity": "Increment Qty",
    "CanDecrementQuantity": "Decrement Qty",
    "CanDeleteItem": "Delete Item",
    "CanApplyItemDiscount": "Apply Discount",
    "CanEditItemComment": "Edit Comment",

    # Section 4 (Actions)
    "CanPrintOrder": "Print Order",
    "CanWaitingOrder": "Send to Waiting",
    "CanCancelOrder": "Cancel Entire Order",

    # Footer Buttons
    "CanAccessFooterDiscountBtn": "Footer Discount",
    "CanAccessFooterCustomerDataBtn": "Footer Customer Data",
    "CanAccessFooterPaymentMethodBtn": "Footer Payment Method",
    "CanAccessFooterQuickPaymentBtn": "Footer Quick Payment",
    "CanAccessFooterMealsBtn": "Footer Meals",
    "CanAccessFooterWaitingBtn": "Footer Waiting List",
    "CanAccessFooterSettingsBtn": "Footer Settings",

    # Sub-Actions (The ones I just detailed)
    "CanCompleteWaitingOrder": "Complete Waiting Order",
    "CanRemoveWaitingOrder": "Remove Waiting Order",
    "CanViewSummaryDetails": "View Summary Details",
    "CanPrintSummaryReport": "Print Summary Report",
    "CanViewOrderDetails": "View Order Details",
    "CanPrintOrderReceipt": "Reprint Order Receipt",
    "CanVoidOrderFromList": "Void Order from List",
    "CanViewStaffAccounts": "View Staff Accounts",
    "CanPrintStaffAccounts": "Print Staff Accounts",

    # DineIn Specific
    "CanAccessDineInOrderBtn": "DineIn Order",
    "CanAccessDineInReceiptBtn": "DineIn Receipt",
    "CanAccessDineInCloseTableBtn": "DineIn Close Table",
    "CanAccessDineInSplitOrderBtn": "DineIn Split Order",
    "CanAccessDineInMergeTableBtn": "DineIn Merge Table",
    "CanAccessDineInTransferBtn": "DineIn Transfer",
    "CanAccessDineInVoidBtn": "DineIn Void",
    "CanAccessDineInGuestCountBtn": "DineIn Guest Count",

    # Delivery Specific
    "CanAccessDeliveryOrderBtn": "Delivery Order",
    "CanAccessDeliveryAddNewBtn": "Delivery Add New",
    "CanAccessDeliveryClearBtn": "Delivery Clear",
    "CanAccessDeliveryComplaintsBtn": "Delivery Complaints",
    "CanAccessDeliverySearchBtn": "Delivery Search",
    "CanAccessDeliveryBranchManagementBtn": "Delivery Branch Mgmt",
    "CanAccessDeliveryDistributionBtn": "Delivery Distribution",
    "CanAccessDeliveryToggleDirectionBtn": "Delivery Toggle Dir",

    # Distribution Specific
    "CanAccessDistributionAssignBtn": "Dist. Assign",
    "CanAccessDistributionViewBtn": "Dist. View",
    "CanAccessDistributionVoidBtn": "Dist. Void",
    "CanAccessDistributionPrintBtn": "Dist. Print",
    "CanAccessDistributionUnDispatchBtn": "Dist. UnDispatch",
    "CanAccessDistributionCollectBtn": "Dist. Collect",
    "CanAccessDistributionVoidHistoryBtn": "Dist. Void History",
    "CanAccessDistributionDriverSettlementBtn": "Dist. Driver Settlement",
    "CanAccessDistributionViewDriversBtn": "Dist. View Drivers",
    
    # Global
    "CanAccessPosSettingsFeature": "POS Settings Feature"
}

def clean_permissions():
    # 1. Update Permissions.cs
    cs_path = r'f:\PointOfSaleProject\src\POS.Authorization\Models\Permissions.cs'
    with open(cs_path, 'w', encoding='utf-8') as f:
        f.write('namespace POS.Authorization.Models;\n\n')
        f.write('public static class Permissions\n{\n')
        f.write('    public static readonly Dictionary<string, string> RolePermissions = new()\n    {\n')
        for key in sorted(golden_set.keys()):
            f.write(f'        {{ "{key}", "{key}" }},\n')
        f.write('    };\n}\n')

    # 2. Update permissions.json (only modify POS ones, keep BackOffice ones if any)
    json_path = r'f:\PointOfSaleProject\src\Pos.Repository\Data\DataSeed\JsonFiles\permissions.json'
    with open(json_path, 'r', encoding='utf-8') as f:
        data = json.load(f)
    
    # Filter out any that were "IsBackOffice: false" but are NOT in our golden set
    new_data = []
    # Add all BackOffice ones back
    for item in data:
        if item.get("IsBackOffice", False):
            new_data.append(item)
    
    # helper for names (Arabic mapping) - I'll try to preserve existing Arabic names if possible
    existing_meta = {item["Name"]: item for item in data}

    for key in sorted(golden_set.keys()):
        if key in existing_meta:
            meta = existing_meta[key]
            meta["IsBackOffice"] = False
            new_data.append(meta)
        else:
            # New one, needs metadata
            new_data.append({
                "Name": key,
                "PoliceArabicName": golden_set[key], # Fallback to English for now, script should handle translation or user edit
                "PoliceEnglishNameEn": golden_set[key],
                "ScreenArabicName": "POS Actions",
                "ScreenEnglishName": "POS Actions",
                "IsBackOffice": False
            })

    with open(json_path, 'w', encoding='utf-8') as f:
        json.dump(new_data, f, ensure_ascii=False, indent=4)

if __name__ == "__main__":
    clean_permissions()
