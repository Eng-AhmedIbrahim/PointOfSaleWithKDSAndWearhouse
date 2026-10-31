import json

# Define new specific permissions to add to roles
detailed_perms = [
    # Waiting
    "CanCompleteWaitingOrder", "CanRemoveWaitingOrder",
    # Summary
    "CanViewSummaryDetails", "CanPrintSummaryReport",
    # All Orders
    "CanViewOrderDetails", "CanPrintOrderReceipt", "CanVoidOrderFromList",
    # Accounts
    "CanViewStaffAccounts", "CanPrintStaffAccounts"
]

def update_seed_file():
    path = r'f:\PointOfSaleProject\src\Pos.Repository\Identity\AppIdentityDbContextSeed.cs'
    with open(path, 'r', encoding='utf-8') as f:
        content = f.read()

    # Add to Branch Manager (مدير فرع) - Gets everything
    bm_marker = '{ "مدير فرع", new List<string> {'
    if bm_marker in content:
        # Just append them to the list
        all_new = ", ".join([f'"{p}"' for p in detailed_perms])
        content = content.replace(bm_marker, bm_marker + "\n                " + all_new + ",")

    # Add relevant ones to Assistant Manager (مساعد مدير)
    am_marker = '{ "مساعد مدير", new List<string> {'
    if am_marker in content:
        am_perms = [
            "CanCompleteWaitingOrder", "CanViewSummaryDetails", "CanPrintSummaryReport",
            "CanViewOrderDetails", "CanPrintOrderReceipt", "CanViewStaffAccounts", "CanPrintStaffAccounts"
        ]
        am_new = ", ".join([f'"{p}"' for p in am_perms])
        content = content.replace(am_marker, am_marker + "\n                " + am_new + ",")

    # Add to Cashier (كاشير) - Limited
    c_marker = '{ "كاشير", new List<string> {'
    if c_marker in content:
        c_perms = ["CanCompleteWaitingOrder", "CanViewOrderDetails", "CanPrintOrderReceipt"]
        c_new = ", ".join([f'"{p}"' for p in c_perms])
        content = content.replace(c_marker, c_marker + "\n                " + c_new + ",")

    with open(path, 'w', encoding='utf-8') as f:
        f.write(content)

if __name__ == "__main__":
    update_seed_file()
