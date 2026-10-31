import os
import re

pos_desktop_path = r'f:\PointOfSaleProject\src\POS.Desktop'
authorization_path = r'f:\PointOfSaleProject\src\POS.Authorization\Models\Permissions.cs'

def get_defined_permissions():
    perms = []
    with open(authorization_path, 'r', encoding='utf-8') as f:
        content = f.read()
        # Find all keys in the dictionary: { "Key", "Value" }
        matches = re.findall(r'\{\s*"([^"]+)"\s*,\s*"[^"]*"\s*\}', content)
        perms.extend(matches)
    return set(perms)

def find_used_permissions(defined_perms):
    used = {}
    for root, dirs, files in os.walk(pos_desktop_path):
        for file in files:
            if file.endswith(('.razor', '.razor.cs', '.cs')):
                path = os.path.join(root, file)
                with open(path, 'r', encoding='utf-8', errors='ignore') as f:
                    content = f.read()
                    for perm in defined_perms:
                        if perm in content:
                            if perm not in used:
                                used[perm] = []
                            used[perm].append(file)
    return used

if __name__ == "__main__":
    defined = get_defined_permissions()
    used = find_used_permissions(defined)
    
    with open(r'f:\PointOfSaleProject\src\analysis_perms.txt', 'w', encoding='utf-8') as out:
        out.write("--- Permissions REVIEW ---\n")
        out.write(f"Total Defined in Permissions.cs: {len(defined)}\n")
        out.write(f"Total Used in POS.Desktop: {len(used)}\n")
        
        unused = defined - set(used.keys())
        out.write("\nUNUSED Permissions (Defined but not found in POS.Desktop):\n")
        for p in sorted(unused):
            out.write(f" - {p}\n")
            
        out.write("\nUSED Permissions and where:\n")
        for p in sorted(used.keys()):
            out.write(f" - {p}: {', '.join(set(used[p]))}\n")
