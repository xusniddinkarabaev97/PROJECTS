import os
import sys
from flask import Flask, render_template, request

# Add parent directory to path so we can import app.py
sys.path.append(os.path.abspath(os.path.join(os.path.dirname(__file__), '..')))

import app as asseto_app
from app import app, ROLES, CATEGORIES, CONDITIONS, STATUSES

def test_role_dashboards():
    print("Starting template rendering dry-run tests...")
    
    # We will mock the database query responses if needed, but since we are running within the app context,
    # let's see if we can perform a simple render.
    # Let's mock request.current_user and monkeypatch get_current_user.
    
    # Save original function
    orig_get_current_user = asseto_app.get_current_user
    
    mock_user = {
        "id": 1,
        "name": "Тестовый Пользователь",
        "email": "test@asseto.ru",
        "role": "superadmin",
        "department": "IT",
        "active": 1,
        "onboarding_done": 1,
        "avatar_color": "#007AFF",
        "expires_at": None,
        "token_version": 0
    }
    
    # Monkeypatch
    asseto_app.get_current_user = lambda: mock_user

    roles_to_test = list(ROLES.keys())
    print(f"Roles to test: {roles_to_test}\n")
    
    success_count = 0
    fail_count = 0
    
    with app.test_request_context('/dashboard'):
        # Mock the request.current_user directly too
        for role in roles_to_test:
            print(f"--- Testing Role: {role} ---")
            mock_user["role"] = role
            request.current_user = mock_user
            
            # Call the /dashboard route function directly
            try:
                # Need to check what variables are passed and make sure no db dependencies fail
                # The index() function queries db for:
                # - all_emps: list of strings
                # - employees: list of dicts
                # Let's run it!
                html = asseto_app.index()
                print(f"  [+] Success rendering dashboard for role '{role}'")
                success_count += 1
            except Exception as e:
                print(f"  [!] FAILED rendering dashboard for role '{role}': {e}")
                import traceback
                traceback.print_exc()
                fail_count += 1
                
    # Restore original function
    asseto_app.get_current_user = orig_get_current_user
    
    print("\n-------------------------------------------")
    print(f"Tests finished: {success_count} passed, {fail_count} failed.")
    print("-------------------------------------------")
    if fail_count > 0:
        sys.exit(1)
    else:
        sys.exit(0)

if __name__ == '__main__':
    test_role_dashboards()
