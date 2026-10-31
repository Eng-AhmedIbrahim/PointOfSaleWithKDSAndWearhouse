import json

# New permissions mapping
new_perms = {
    "CanUseKeypad": "Use Keypad",
    "CanIncrementQuantity": "Increment Quantity",
    "CanDecrementQuantity": "Decrement Quantity",
    "CanDeleteItem": "Remove Item",
    "CanApplyItemDiscount": "Item Discount",
    "CanEditItemComment": "Edit Item Notes",
    "CanCancelOrder": "Cancel Entire Order",
    "CanWaitingOrder": "Send to Waiting",
    "CanPrintOrder": "Print Order"
}

def update_seed():
    path = r'f:\PointOfSaleProject\src\Pos.Repository\Identity\AppIdentityDbContextSeed.cs'
    with open(path, 'r', encoding='utf-8') as f:
        content = f.read()

    # Find the Roles sections
    # Managers (A - Branch Manager) should get all
    # Managers (B - Asst Assistant Manager) should get most
    # Cashier should get some
    
    # Let's add them to Administrator dynamically via the logic that already exists (it adds all perms)
    # But for the hardcoded lists:
    
    # 1. Branch Manager (مدير فرع)
    for perm in new_perms.keys():
        if f'"{perm}"' not in content:
            # Insert before "CanAccessVoidOrder" or at start of list
            content = content.replace('// POS Actions\n                "CanAccessVoidOrder"', f'// POS Actions\n                "{perm}", "CanAccessVoidOrder"')

    # 2. Assistant Manager (مساعد مدير)
    for perm in new_perms.keys():
         content = content.replace('// POS Actions\n                "CanAccessTransferTable"', f'// POS Actions\n                "{perm}", "CanAccessTransferTable"')

    # 3. Cashier (كاشير)
    cashier_perms = '"{0}", "{1}", "{2}", "{3}", "{4}"'.format(
        "CanUseKeypad", "CanIncrementQuantity", "CanDecrementQuantity", "CanPrintOrder", "CanWaitingOrder"
    )
    content = content.replace('// POS Actions\n                "CanAccessDiscount"', f'// POS Actions\n                {cashier_perms}, "CanAccessDiscount"')

    with open(path, 'w', encoding='utf-8') as f:
        f.write(content)

if __name__ == "__main__":
    update_seed()
