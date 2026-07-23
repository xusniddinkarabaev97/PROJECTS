import paramiko

c = paramiko.SSHClient()
c.set_missing_host_key_policy(paramiko.AutoAddPolicy())
c.connect('45.150.25.74', port=3422, username='user', password='P@$$w0rdUNICON123', timeout=10)

stdin, stdout, stderr = c.exec_command("""
echo "=== Check /asseto/12345 ==="
ls -la /asseto/12345/ 2>/dev/null || echo "No /asseto/12345"
ls -la /home/user/asseto/12345/ 2>/dev/null || echo "No /home/user/asseto/12345"
ls -la /opt/asseto/ 2>/dev/null || echo "No /opt/asseto"
find / -maxdepth 4 -name '12345' -type d 2>/dev/null | head -5
""")
print(stdout.read().decode())
c.close()
