
import sys
sys.path.insert(0, '.')
os.chdir('.')

# Monkey-patch before importing app
import modules.auth

orig = modules.auth._attach_csrf_cookie

def debug_csrf(resp):
    from flask import request
    csrf = request.cookies.get('csrf_token')
    with open('csrf_debug.log', 'a') as f:
        f.write(f'{request.method} {request.path}: csrf={repr(csrf)[:50]}  cookies={list(request.cookies.keys())}
')
    return orig(resp)

modules.auth._attach_csrf_cookie = debug_csrf

from modules.config import app
app.run(debug=False, host='0.0.0.0', port=5001, threaded=True)
