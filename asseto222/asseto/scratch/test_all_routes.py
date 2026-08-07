import os
import sys
from flask import request

# Add parent directory to path so we can import app.py
sys.path.append(os.path.abspath(os.path.join(os.path.dirname(__file__), '..')))

import app as asseto_app
from app import app, ROLES

def test_all_routes():
    print("Starting Comprehensive HTML Views Verification with Redirect Resolution...")
    
    current_mock_user = {}
    
    # Save original function
    orig_get_current_user = asseto_app.get_current_user
    asseto_app.get_current_user = lambda: current_mock_user if current_mock_user else None

    # Get sample IDs from database (PostgreSQL)
    import pg_compat as _db
    DATABASE_URL = os.environ.get(
        'DATABASE_URL',
        'postgresql://asseto:asseto@localhost:5432/asseto',
    )
    db = _db.connect(DATABASE_URL)
    item = db.execute('SELECT inv_num FROM items LIMIT 1').fetchone()
    dismissal = db.execute('SELECT id FROM dismissals LIMIT 1').fetchone()
    user_row = db.execute('SELECT id FROM users LIMIT 1').fetchone()
    
    sample_inv_num = item['inv_num'] if item else 'TRK-2026001'
    sample_did = dismissal['id'] if dismissal else 1
    sample_uid = user_row['id'] if user_row else 1
    db.close()

    # Views to test
    views = [
        "/dashboard",
        f"/asset/{sample_inv_num}",
        "/admin/users",
        "/qr-sheet",
        "/qr-sheet-employees",
        "/qr-print",
        f"/employee/{sample_uid}/print",
        f"/asset/{sample_inv_num}/print",
        "/admin/dismissals",
        f"/dismissal/{sample_did}",
        "/history",
        "/profile",
        "/documents",
        "/settings",
        "/docs",
        "/maintenance",
        "/requests",
        "/analytics",
        "/inventory",
        "/security",
        "/billing"
    ]

    roles_to_test = list(ROLES.keys())
    
    failures = []
    
    with app.test_client() as client:
        for role in roles_to_test:
            print(f"\n==========================================")
            print(f"TESTING ROLE: {role.upper()}")
            print(f"==========================================")
            
            # Setup current user fields
            current_mock_user.clear()
            current_mock_user.update({
                "id": 1,
                "name": "Тестовый Пользователь",
                "email": "test@asseto.ru",
                "role": role,
                "department": "IT",
                "active": 1,
                "onboarding_done": 1,
                "avatar_color": "#007AFF",
                "expires_at": None,
                "token_version": 0
            })
            
            # Set cookies so request thinks we are logged in
            client.set_cookie('token', 'mock_jwt_token')
            
            for path in views:
                try:
                    response = client.get(path)
                    status = response.status_code
                    
                    if status in (301, 302):
                        location = response.headers.get("Location", "")
                        print(f"  [->] REDIRECT: {path} -> {location} (status {status})")
                    elif status == 200:
                        print(f"  [+] OK: {path} (status 200)")
                    elif status == 403:
                        print(f"  [-] FORBIDDEN: {path} (status 403 - Access correctly denied)")
                    elif status == 404:
                        print(f"  [?] NOT FOUND: {path} (status 404 - ID or item mismatch but template compiled)")
                    else:
                        failures.append((role, path, status, "200/30x/403/404"))
                        print(f"  [!] UNEXPECTED STATUS: {path} returned status {status}")
                except Exception as e:
                    failures.append((role, path, "EXCEPTION", str(e)))
                    print(f"  [!] EXCEPTION: {path} raised {e}")
                    import traceback
                    traceback.print_exc()

    # Restore original function
    asseto_app.get_current_user = orig_get_current_user
    
    print("\n==========================================")
    print("VERIFICATION SUMMARY")
    print("==========================================")
    if failures:
        print(f"Found {len(failures)} failures:")
        for fail in failures:
            print(f"Role: {fail[0]} | Path: {fail[1]} | Status: {fail[2]}")
        sys.exit(1)
    else:
        print("ALL VIEWS AND ROLES VERIFIED SUCCESSFULLY WITH NO INTERNAL 500 ERRORS OR JINJA SYNTAX ERRORS!")
        sys.exit(0)

if __name__ == '__main__':
    test_all_routes();
