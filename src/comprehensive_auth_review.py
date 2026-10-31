import os
import re

pos_desktop_path = r'f:\PointOfSaleProject\src\POS.Desktop'

def find_all_authorization_strings():
    auth_strings = set()
    
    # Pattern 1: [Authorize(Policy = "PermissionName")] or [Authorize("PermissionName")]
    # Pattern 2: (await AuthorizationService.AuthorizeAsync(user, "PermissionName"))
    # Pattern 3: <AuthorizeView Policy="PermissionName">
    
    patterns = [
        r'AuthorizeAsync\(\s*[^,]+,\s*"([^"]+)"\s*\)',
        r'\[Authorize\(\s*Policy\s*=\s*"([^"]+)"\s*\)\]',
        r'\[Authorize\("\s*([^"]+)"\s*\)\]',
        r'Policy\s*=\s*"([^"]+)"',
        r'IsFeatureEnabled\("\s*([^"]+)"\s*\)'
    ]
    
    for root, dirs, files in os.walk(pos_desktop_path):
        for file in files:
            if file.endswith(('.razor', '.razor.cs', '.cs')):
                path = os.path.join(root, file)
                with open(path, 'r', encoding='utf-8', errors='ignore') as f:
                    content = f.read()
                    for pattern in patterns:
                        matches = re.findall(pattern, content)
                        auth_strings.update(matches)
                        
    return auth_strings

if __name__ == "__main__":
    found_in_code = find_all_authorization_strings()
    
    # Also get defined ones
    authorization_path = r'f:\PointOfSaleProject\src\POS.Authorization\Models\Permissions.cs'
    defined = set()
    with open(authorization_path, 'r', encoding='utf-8') as f:
        content = f.read()
        matches = re.findall(r'\{\s*"([^"]+)"\s*,\s*"[^"]*"\s*\}', content)
        defined.update(matches)
        
    with open(r'f:\PointOfSaleProject\src\comprehensive_auth_review.txt', 'w', encoding='utf-8') as out:
        out.write("--- COMPREHENSIVE AUTHORIZATION REVIEW ---\n")
        out.write(f"Total Unique Auth Strings Found in POS.Desktop Code: {len(found_in_code)}\n")
        out.write(f"Total Permissions Defined in Permissions.cs: {len(defined)}\n\n")
        
        missing_from_cs = found_in_code - defined
        out.write("MISSING from Permissions.cs (Used in code but not defined):\n")
        for p in sorted(missing_from_cs):
            out.write(f" - {p}\n")
            
        out.write("\nUNUSED in POS.Desktop (Defined in CS but not found in code):\n")
        unused = defined - found_in_code
        for p in sorted(unused):
            out.write(f" - {p}\n")
            
        out.write("\nMATCHED (Both in code and defined):\n")
        matched = found_in_code & defined
        for p in sorted(matched):
            out.write(f" - {p}\n")
            
        # Also check features
        out.write("\nFEATURE Strings (IsFeatureEnabled):\n")
        feature_strings = [s for s in found_in_code if "POS_" in s]
        for s in sorted(feature_strings):
            out.write(f" - {s}\n")
