"""
Dlpdpi Agent Uninstaller — standalone EXE
Double-click to uninstall. Auto-requests admin rights.
Notifies server before removing local files.
"""
import sys, os, subprocess, urllib.request, json, shutil

SERVER = "http://45.150.25.74:80"
TASK_NAME = "DlpdpiEndpointAgent"
AGENT_DIR = os.path.join(os.environ.get("ProgramData", "C:/ProgramData"), "DlpdpiAgent")

def is_admin():
    try:
        return bool(subprocess.run(["net", "session"], capture_output=True).returncode == 0)
    except:
        return False

def run_as_admin():
    if not is_admin():
        print("[*] Requesting Administrator rights...")
        import ctypes
        ctypes.windll.shell32.ShellExecuteW(
            None, "runas", sys.executable, f'"{__file__}"', None, 1
        )
        sys.exit(0)

def main():
    run_as_admin()
    print("=" * 50)
    print("  Dlpdpi Agent Uninstaller")
    print("=" * 50)
    print()

    hostname = os.environ.get("COMPUTERNAME", "UNKNOWN")
    agent_id = f"agent-{hostname}"

    # 1. Notify server
    print("[1/4] Notifying server...")
    try:
        body = json.dumps({
            "status": "offline",
            "hostname": hostname
        }).encode()
        req = urllib.request.Request(
            f"{SERVER}/api/v1/agents/{agent_id}/heartbeat",
            data=body,
            headers={"Content-Type": "application/json"},
            method="POST"
        )
        r = urllib.request.urlopen(req, timeout=10)
        print(f"      Server notified (HTTP {r.status})")
    except urllib.error.HTTPError as e:
        if e.code == 404:
            print("      Agent not registered on server")
        else:
            print(f"      Server: HTTP {e.code}")
    except Exception as e:
        print(f"      WARNING: Server unreachable ({e})")
        print("      Agent will be removed locally anyway")

    # 2. Delete scheduled task
    print("[2/4] Removing scheduled task...")
    r = subprocess.run(
        ["schtasks", "/delete", "/tn", TASK_NAME, "/f"],
        capture_output=True, text=True
    )
    if r.returncode == 0:
        print("      Task deleted")
    else:
        print("      Task not found (already removed)")

    # 3. Stop and delete service
    print("[3/4] Removing Windows service...")
    subprocess.run(["sc", "stop", "DlpdpiEndpoint"], capture_output=True)
    r = subprocess.run(["sc", "delete", "DlpdpiEndpoint"], capture_output=True, text=True)
    if "SUCCESS" in r.stdout or "not exist" in r.stdout.lower():
        print("      Service removed")
    else:
        print("      Service not found")

    # 4. Remove files
    print("[4/4] Removing agent files...")
    if os.path.exists(AGENT_DIR):
        shutil.rmtree(AGENT_DIR, ignore_errors=True)
        print(f"      Deleted: {AGENT_DIR}")
    else:
        print(f"      Not found: {AGENT_DIR}")

    # Clean log
    log = os.path.join(os.environ.get("TEMP", ""), "dlpdpi_agent.log")
    if os.path.exists(log):
        os.remove(log)
        print(f"      Deleted log: {log}")

    print()
    print("=" * 50)
    print("  UNINSTALL COMPLETE!")
    print(f"  Agent '{hostname}' removed.")
    print("=" * 50)
    print()
    input("Press Enter to exit...")

if __name__ == "__main__":
    main()
