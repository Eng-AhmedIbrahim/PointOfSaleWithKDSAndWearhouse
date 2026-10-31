path = r"f:\PointOfSaleProject\src\Pos.Repository\Identity\AppIdentityDbContextSeed.cs"

with open(path, "r", encoding="utf-8") as f:
    content = f.read()

target = '"CanViewSummaryDetails", "CanPrintSummaryReport",'
replacement = '"CanViewSummaryDetails", "CanViewDetailedSales", "CanViewSalesItems", "CanPrintSummaryReport",'

if target in content:
    content = content.replace(target, replacement)
    
    with open(path, "w", encoding="utf-8") as f:
        f.write(content)
    print("Replaced successfully")
else:
    print("Target string not found in file")
