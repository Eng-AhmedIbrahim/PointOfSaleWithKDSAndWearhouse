import re

seed_path = 'f:/PointOfSaleProject/src/POS.Repository/Data/DataSeed/AppIdentityDbContextSeed.cs'
with open(seed_path, 'r', encoding='utf-8') as f:
    code = f.read()

# I need to add \"CanViewSalesItemsAtBackOffice\" and \"CanPrintSalesItemsAtBackOffice\"
# to BranchManager and Assistant permissions

if '\"CanViewSalesItemsAtBackOffice\"' not in code:
    code = code.replace(', \"CanPrintSummaryReportAtBackOffice\"', 
                       ', \"CanPrintSummaryReportAtBackOffice\", \"CanViewSalesItemsAtBackOffice\", \"CanPrintSalesItemsAtBackOffice\"')

with open(seed_path, 'w', encoding='utf-8') as f:
    f.write(code)

print('Updated seed')
