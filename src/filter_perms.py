import os
import json
import re

# Paths
root_dir = r'f:\PointOfSaleProject\src'
permissions_class_path = os.path.join(root_dir, 'POS.Authorization', 'Models', 'Permissions.cs')
json_path = os.path.join(root_dir, 'Pos.Repository', 'Data', 'DataSeed', 'JsonFiles', 'permissions.json')

# 1. Extract permission names from Permissions.cs
perm_names = []
if os.path.exists(permissions_class_path):
    with open(permissions_class_path, 'r', encoding='utf-8') as f:
        content = f.read()
        # Find matches like { "Name", "Name" }
        matches = re.findall(r'\{\s*"([^"]+)"\s*,\s*"[^"]+"\s*\}', content)
        perm_names = list(set(matches))

print(f"Found {len(perm_names)} permissions in Permissions.cs")

# 2. Find usages in all .razor files
used_perms = set()
for root, dirs, files in os.walk(root_dir):
    for file in files:
        if file.endswith('.razor') or file.endswith('.razor.cs'):
            file_path = os.path.join(root, file)
            try:
                with open(file_path, 'r', encoding='utf-8') as f:
                    file_content = f.read()
                    for perm in perm_names:
                        if perm in file_content:
                            used_perms.add(perm)
            except:
                pass

print(f"Found {len(used_perms)} permissions used in .razor files")

# 3. Filter permissions.json
if os.path.exists(json_path):
    with open(json_path, 'r', encoding='utf-8') as f:
        data = json.load(f)
    
    # We keep it if it's used in razor OR if it's one of the core screen ones we just fixed 
    # (maybe the user wants to keep the screen ones even if not yet fully authorized in razor?)
    # Actually, the user was very specific: "شيل كل الحاجات ال مش مستخده ف الريزور"
    
    filtered_data = [item for item in data if item.get("Name") in used_perms]
    
    print(f"Filtering {len(data)} items down to {len(filtered_data)}")
    
    with open(json_path, 'w', encoding='utf-8') as f:
        json.dump(filtered_data, f, ensure_ascii=False, indent=4)
else:
    print("permissions.json not found")
