#!/usr/bin/env python3
"""Deploy Whirl site to Huawei Cloud Stack ECS"""

import glob
import io
import os
import tempfile

import paramiko

HOSTS = ["87.192.236.241"]  # public EIP
USER = "root"
KEY_HEX = "54565aad12d3c4f1a14b904202020fd6fd1ac7a8492dfe2bed0dff1ea1009ce1"
PASSWORD = "Qwerty123!"

SITE_DIR = "/var/www/whirl"
NGINX_CONF_PATH = "/etc/nginx/sites-available/whirl"
NGINX_ENABLED = "/etc/nginx/sites-enabled/whirl"
LOCAL_DIR = os.path.dirname(os.path.abspath(__file__))


def try_keys(host):
    # --- Method 1: ED25519 from seed ---
    try:
        from cryptography.hazmat.primitives import serialization
        from cryptography.hazmat.primitives.asymmetric.ed25519 import Ed25519PrivateKey

        seed = bytes.fromhex(KEY_HEX)
        priv = Ed25519PrivateKey.from_private_bytes(seed)
        pem = priv.private_bytes(
            encoding=serialization.Encoding.PEM,
            format=serialization.PrivateFormat.OpenSSH,
            encryption_algorithm=serialization.NoEncryption(),
        )
        with tempfile.NamedTemporaryFile(suffix=".pem", delete=False, mode="w") as f:
            f.write(pem.decode())
            tmp = f.name
        try:
            key = paramiko.Ed25519Key.from_private_key_file(tmp)
            client = paramiko.SSHClient()
            client.set_missing_host_key_policy(paramiko.AutoAddPolicy())
            client.connect(
                host,
                username=USER,
                pkey=key,
                timeout=15,
                allow_agent=False,
                look_for_keys=False,
            )
            os.unlink(tmp)
            print(f"  Connected to {host} (ED25519)")
            return client
        except Exception as e:
            os.unlink(tmp)
            # fall through
            pass
    except Exception as e:
        pass

    # --- Method 2: Try as PEM directly ---
    # Some keys are raw PEM without headers
    for prefix in [
        "-----BEGIN RSA PRIVATE KEY-----\n",
        "-----BEGIN OPENSSH PRIVATE KEY-----\n",
        "-----BEGIN PRIVATE KEY-----\n",
    ]:
        for suffix in [
            "\n-----END RSA PRIVATE KEY-----",
            "\n-----END OPENSSH PRIVATE KEY-----",
            "\n-----END PRIVATE KEY-----",
        ]:
            try:
                pem = prefix + KEY_HEX + suffix
                with tempfile.NamedTemporaryFile(
                    suffix=".pem", delete=False, mode="w"
                ) as f:
                    f.write(pem)
                    tmp = f.name
                pkey = None
                for key_cls in [
                    paramiko.RSAKey,
                    paramiko.Ed25519Key,
                    paramiko.ECDSAKey,
                ]:
                    try:
                        pkey = key_cls.from_private_key_file(tmp)
                        break
                    except:
                        continue
                if pkey:
                    client = paramiko.SSHClient()
                    client.set_missing_host_key_policy(paramiko.AutoAddPolicy())
                    client.connect(
                        host,
                        username=USER,
                        pkey=pkey,
                        timeout=15,
                        allow_agent=False,
                        look_for_keys=False,
                    )
                    os.unlink(tmp)
                    print(f"  Connected to {host} (PEM-wrapped)")
                    return client
                os.unlink(tmp)
            except:
                if os.path.exists(tmp):
                    os.unlink(tmp)
                continue

    # --- Method 3: Password ---
    try:
        client = paramiko.SSHClient()
        client.set_missing_host_key_policy(paramiko.AutoAddPolicy())
        client.connect(
            host,
            username=USER,
            password=PASSWORD,
            timeout=15,
            allow_agent=False,
            look_for_keys=False,
        )
        print(f"  Connected to {host} (password)")
        return client
    except:
        pass

    return None


def run_cmd(client, cmd, desc=""):
    if desc:
        print(f"  {desc}...", end=" ", flush=True)
    stdin, stdout, stderr = client.exec_command(cmd)
    exit_code = stdout.channel.recv_exit_status()
    out = stdout.read().decode()
    err = stderr.read().decode()
    if exit_code != 0 and err.strip() and "WARNING" not in err:
        if desc:
            print("FAIL")
        print(f"    ERROR: {err.strip()}")
        return False
    if desc:
        print("OK")
    return True


def upload_file(sftp, local, remote, desc=""):
    if desc:
        print(f"  Upload: {desc}...", end=" ", flush=True)
    sftp.put(local, remote)
    print("OK")


def main():
    print(f"=== Whirl Deploy to HCS ECS ===\n")

    print("1. Connecting...")
    client = None
    active_host = None
    for host in HOSTS:
        print(f"  Trying {host}...", end=" ", flush=True)
        client = try_keys(host)
        if client:
            active_host = host
            break
        print("FAILED")

    if not client:
        print("\n  ERROR: Could not connect. Check key and IP.")
        return

    print("2. Setting up server...")
    run_cmd(
        client,
        "which nginx || (apt-get update -qq && apt-get install -y -qq nginx)",
        "Install nginx",
    )
    run_cmd(client, f"mkdir -p {SITE_DIR}", "Create site dir")

    print("3. Uploading files...")
    sftp = client.open_sftp()
    for f in glob.glob(os.path.join(LOCAL_DIR, "*.html")):
        upload_file(
            sftp, f, os.path.join(SITE_DIR, os.path.basename(f)), os.path.basename(f)
        )
    for f in glob.glob(os.path.join(LOCAL_DIR, "*.pdf")):
        upload_file(
            sftp, f, os.path.join(SITE_DIR, os.path.basename(f)), os.path.basename(f)
        )
    upload_file(
        sftp, os.path.join(LOCAL_DIR, "nginx.conf"), NGINX_CONF_PATH, "nginx.conf"
    )
    sftp.close()

    run_cmd(client, f"chown -R www-data:www-data {SITE_DIR}", "Set ownership")
    run_cmd(client, f"chmod -R 755 {SITE_DIR}", "Set permissions")
    run_cmd(client, f"ln -sf {NGINX_CONF_PATH} {NGINX_ENABLED}", "Enable site")
    run_cmd(client, "rm -f /etc/nginx/sites-enabled/default", "Remove default")

    if run_cmd(client, "nginx -t", "Test nginx"):
        run_cmd(
            client, "systemctl reload nginx || service nginx reload", "Reload nginx"
        )
        print(f"\n{'=' * 50}")
        print(f"  DEPLOY SUCCESS!")
        print(f"  Site: http://{active_host}")
        print(f"{'=' * 50}")

    client.close()


if __name__ == "__main__":
    main()
