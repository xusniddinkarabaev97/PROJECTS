import paramiko

host = "45.150.25.74"
port = 3422
username = "user"
password = "P@$$w0rdUNICON123"

script = r"""
echo "=== Check folder 12345 ==="
ls -la /12345 2>/dev/null || echo "No /12345"
ls -la /home/user/12345 2>/dev/null || echo "No /home/user/12345"
ls -la /opt/12345 2>/dev/null || echo "No /opt/12345"
find / -maxdepth 3 -name '12345' -type d 2>/dev/null | head -5

echo ""
echo "=== Check for other nginx/openresty processes ==="
ps aux | grep -E 'nginx|openresty' | grep -v grep | head -10

echo ""
echo "=== Check all listening ports ==="
ss -tlnp 2>/dev/null | head -20 || netstat -tlnp 2>/dev/null | head -20

echo ""
echo "=== Check if there's another HTTP server on port 80 ==="
curl -s http://localhost:80/index-min.html | grep 'apiStatusText' | head -3
"""

client = paramiko.SSHClient()
client.set_missing_host_key_policy(paramiko.AutoAddPolicy())

try:
    client.connect(host, port=port, username=username, password=password, timeout=10)
    stdin, stdout, stderr = client.exec_command(script)
    print(stdout.read().decode())
    err = stderr.read().decode()
    if err:
        print("STDERR:", err)
except Exception as e:
    print(f"ERROR: {e}")
finally:
    client.close()
