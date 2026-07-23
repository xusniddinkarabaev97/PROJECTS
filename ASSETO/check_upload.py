import paramiko, time

c = paramiko.SSHClient()
c.set_missing_host_key_policy(paramiko.AutoAddPolicy())
c.connect('45.150.25.74', port=3422, username='user', password='P@$$w0rdUNICON123', timeout=10)

# Check multiple possible locations
stdin, stdout, stderr = c.exec_command("""
echo "=== /home/user/v3 ==="
ls -la /home/user/v3/ 2>/dev/null || echo "Not found"

echo ""
echo "=== /home/user/uploads ==="
ls -la /home/user/uploads/ 2>/dev/null || echo "Not found"

echo ""
echo "=== /home/user/12345 ==="
ls -la /home/user/12345/ 2>/dev/null || echo "Not found"

echo ""
echo "=== Any new zip files in /home/user ==="
find /home/user -maxdepth 2 -name '*.zip' -o -name '*.tar.gz' -newer /tmp/recv.log 2>/dev/null | head -5

echo ""
echo "=== Recv server still running? ==="
ps aux | grep 'recv.py' | grep -v grep | head -1 || echo "Not running"
""")
print(stdout.read().decode())
c.close()
