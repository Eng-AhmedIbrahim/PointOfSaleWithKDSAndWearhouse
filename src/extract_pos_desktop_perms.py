import os
import re

root_dir = r'f:\PointOfSaleProject\src\POS.Desktop'
used_perms = set()

# Pattern to find strings starting with CanAccess
pattern = re.compile(r'CanAccess[a-zA-Z0-9]+')

for root, dirs, files in os.walk(root_dir):
    for file in files:
        if file.endswith(('.razor', '.razor.cs', '.cs')):
            path = os.path.join(root, file)
            try:
                with open(path, 'r', encoding='utf-8') as f:
                    content = f.read()
                    matches = pattern.findall(content)
                    for m in matches:
                        used_perms.add(m)
            except:
                pass

print("--- Used Permissions in POS.Desktop ---")
for p in sorted(list(used_perms)):
    print(p)
