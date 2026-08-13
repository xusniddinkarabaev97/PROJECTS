"""
Dlpdpi Agent — standalone launcher (compiled to EXE)
No Python required on target machine.
"""
import os, sys, time, logging

os.environ.setdefault("DLPDPI_API", "http://45.150.25.74:80")
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

logging.basicConfig(
    filename=os.path.join(os.environ.get("TEMP", "."), "dlpdpi_agent.log"),
    level=logging.INFO,
    format="%(asctime)s %(levelname)s %(message)s"
)

logging.info("Agent starting...")

from endpoint_agent.agent import main

# Run once then exit (scheduled task will restart every 5 min)
try:
    main()
except Exception as e:
    logging.error(f"Agent error: {e}")
