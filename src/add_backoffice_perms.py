import json
import os

permissions_path = 'f:/PointOfSaleProject/src/POS.Repository/Data/DataSeed/JsonFiles/permissions.json'
with open(permissions_path, 'r', encoding='utf-8') as f:
    permissions = json.load(f)

for p in permissions:
    if p["Name"] == "CanViewSummaryDetailsAtBackOffice":
        p["LabelEn"] = "View Detailed Sales (BackOffice)"
        p["LabelAr"] = "??? ???????? ????????? ?? ?????? (?????)"
    if p["Name"] == "CanPrintSummaryReportAtBackOffice":
        p["LabelEn"] = "Print Summary Report (BackOffice)"
        p["LabelAr"] = "????? ???? ???????? (?????)"

new_perms = [
    {
        "Id": 0,
        "Name": "CanViewSalesItemsAtBackOffice",
        "LabelEn": "View Sales Items (BackOffice)",
        "LabelAr": "??? ????? ????? ?? ?????? (?????)",
        "GroupName": "Reporting Actions",
        "IsActive": True
    },
    {
        "Id": 0,
        "Name": "CanPrintSalesItemsAtBackOffice",
        "LabelEn": "Print Sales Items (BackOffice)",
        "LabelAr": "????? ????? ????? ?? ?????? (?????)",
        "GroupName": "Reporting Actions",
        "IsActive": True
    }
]

existing_names = [p["Name"] for p in permissions]
for np in new_perms:
    if np["Name"] not in existing_names:
        permissions.append(np)

# Update IDs
for i, p in enumerate(permissions, 1):
    p["Id"] = i

with open(permissions_path, 'w', encoding='utf-8') as f:
    json.dump(permissions, f, indent=4, ensure_ascii=False)
print('Updated permissions.json')
