"""
Dlpdpi Agent Installer v2 — standalone, no Python required
Bundles the agent EXE directly. Double-click to install.
"""
import sys, os, subprocess, shutil, base64, tempfile

SERVER = "http://45.150.25.74:80"
TASK_NAME = "DlpdpiEndpointAgent"
AGENT_DIR = os.path.join(os.environ.get("ProgramData", "C:/ProgramData"), "DlpdpiAgent")
AGENT_EXE = "DlpdpiAgent.exe"

def is_admin():
    try:
        return subprocess.run(["net", "session"], capture_output=True).returncode == 0
    except:
        return False

def run_as_admin():
    if not is_admin():
        import ctypes
        ctypes.windll.shell32.ShellExecuteW(
            None, "runas", sys.executable, f'"{__file__}"', None, 1
        )
        sys.exit(0)

def main():
    run_as_admin()
    print("=" * 50)
    print("  Dlpdpi Agent Installer v2")
    print("=" * 50)
    print()

    # 1. Stop old agent
    print("[1/4] Stopping old agent...")
    subprocess.run(["schtasks", "/end", "/tn", TASK_NAME], capture_output=True)
    subprocess.run(["schtasks", "/delete", "/tn", TASK_NAME, "/f"], capture_output=True)
    subprocess.run(["sc", "stop", "DlpdpiEndpoint"], capture_output=True)
    subprocess.run(["sc", "delete", "DlpdpiEndpoint"], capture_output=True)
    time.sleep(2)
    print("      OK")

    # 2. Copy agent EXE
    print("[2/4] Installing agent...")
    os.makedirs(AGENT_DIR, exist_ok=True)
    os.makedirs(os.path.join(AGENT_DIR, "quarantine"), exist_ok=True)
    
    # The agent EXE is bundled alongside this installer
    # Look for it next to the installer, or in the same directory
    installer_dir = os.path.dirname(os.path.abspath(__file__))
    agent_src = os.path.join(installer_dir, AGENT_EXE)
    
    if not os.path.exists(agent_src):
        # Try current directory
        agent_src = os.path.join(os.getcwd(), AGENT_EXE)
    
    if os.path.exists(agent_src):
        agent_dst = os.path.join(AGENT_DIR, AGENT_EXE)
        shutil.copy2(agent_src, agent_dst)
        print(f"      Copied to {agent_dst}")
    else:
        print(f"      ERROR: {AGENT_EXE} not found!")
        print(f"      Place {AGENT_EXE} next to this installer.")
        input("Press Enter to exit...")
        sys.exit(1)

    # 3. Create scheduled task
    print("[3/4] Creating scheduled task...")
    agent_path = os.path.join(AGENT_DIR, AGENT_EXE)
    subprocess.run([
        "schtasks", "/create", "/tn", TASK_NAME,
        "/tr", f'"{agent_path}" --once',
        "/sc", "ONSTART", "/ru", "SYSTEM", "/rl", "HIGHEST", "/f"
    ], capture_output=True)
    
    # Add trigger every 5 minutes
    subprocess.run([
        "schtasks", "/change", "/tn", TASK_NAME,
        "/ri", "5", "/du", "24:00"
    ], capture_output=True)
    print("      Task created (runs at startup + every 5 min)")

    # 4. Start agent
    print("[4/4] Starting agent...")
    subprocess.run(["schtasks", "/run", "/tn", TASK_NAME], capture_output=True)
    
    import time as _time
    _time.sleep(3)
    
    # Verify
    r = subprocess.run(["schtasks", "/query", "/tn", TASK_NAME], capture_output=True, text=True)
    if "DlpdpiEndpointAgent" in r.stdout:
        print("      Agent running!")
    else:
        print("      WARNING: Task may not have started")

    print()
    print("=" * 50)
    print("  INSTALL COMPLETE!")
    print(f"  Agent: {agent_path}")
    print(f"  Check: {SERVER} -> Agents tab")
    print("=" * 50)
    print()
    input("Press Enter to exit...")

if __name__ == "__main__":
    main()
