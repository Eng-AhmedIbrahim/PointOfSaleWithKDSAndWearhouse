import os
import re
import json

root_dir = r'f:\PointOfSaleProject\src'
permissions_class_path = os.path.join(root_dir, 'POS.Authorization', 'Models', 'Permissions.cs')
json_path = os.path.join(root_dir, 'Pos.Repository', 'Data', 'DataSeed', 'JsonFiles', 'permissions.json')

# 1. Get all definitions
with open(permissions_class_path, 'r', encoding='utf-8') as f:
    orig_lines = f.readlines()

pattern = re.compile(r'\{\s*"([^"]+)"\s*,\s*"[^"]+"\s*\}')

all_perms = []
for line in orig_lines:
    match = pattern.search(line)
    if match:
        all_perms.append(match.group(1))

# 2. Check usage ONLY in .razor and .razor.cs
used_in_razor = set()
for root, dirs, files in os.walk(root_dir):
    for file in files:
        if file.endswith(('.razor', '.razor.cs')):
            path = os.path.join(root, file)
            try:
                with open(path, 'r', encoding='utf-8') as f:
                    content = f.read()
                    for p in all_perms:
                        if p in content:
                            used_in_razor.add(p)
            except:
                pass

print(f"Perms used in Razor/UI: {len(used_in_razor)}")

# 3. Filter Permissions.cs
new_lines = []
for line in orig_lines:
    match = pattern.search(line)
    if match:
        p_name = match.group(1)
        if p_name in used_in_razor:
            new_lines.append(line)
        else:
            print(f"Removing unused in Razor: {p_name}")
    else:
        new_lines.append(line)

with open(permissions_class_path, 'w', encoding='utf-8') as f:
    f.writelines(new_lines)

# 4. Filter permissions.json
if os.path.exists(json_path):
    with open(json_path, 'r', encoding='utf-8') as f:
        data = json.load(f)
    
    filtered_data = [item for item in data if item.get("Name") in used_in_razor]
    print(f"Filtered JSON from {len(data)} to {len(filtered_data)}")
    
    with open(json_path, 'w', encoding='utf-8') as f:
        json.dump(filtered_data, f, ensure_ascii=False, indent=4)
