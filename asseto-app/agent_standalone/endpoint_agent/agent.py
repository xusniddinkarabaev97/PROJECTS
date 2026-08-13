import argparse
import hashlib
import json
import logging
import os
import platform
import shutil
import socket
import sys
import time
import uuid
from dataclasses import dataclass, field
from datetime import datetime, timezone
from pathlib import Path
from typing import Any, Iterable

import httpx


POLICY_VERSION = "endpoint-agent-mvp-2"
DEFAULT_API = os.getenv("DLPDPI_API", "http://45.150.25.74:80")
DEFAULT_STATE_FILE = Path(os.getenv("DLPDPI_AGENT_STATE", ".endpoint-agent-state.json"))
DEFAULT_ENFORCEMENT_MODE = os.getenv("DLPDPI_ENFORCEMENT_MODE", "monitor")
DEFAULT_QUARANTINE_DIR = os.getenv("DLPDPI_QUARANTINE_DIR")
DEFAULT_POLICY_SOURCE = os.getenv("DLPDPI_POLICY_SOURCE", "gateway")

logging.basicConfig(
    level=os.getenv("LOG_LEVEL", "INFO").upper(),
    format="%(asctime)s %(levelname)s %(name)s %(message)s",
)
LOGGER = logging.getLogger("dlpdpi.endpoint_agent")

RISKY_EXTENSIONS = {
    ".bat",
    ".cmd",
    ".com",
    ".dll",
    ".exe",
    ".hta",
    ".jar",
    ".js",
    ".jse",
    ".lnk",
    ".msi",
    ".ps1",
    ".reg",
    ".scr",
    ".vbe",
    ".vbs",
    ".wsf",
}

SENSITIVE_NAME_KEYWORDS = {
    "confidential",
    "credential",
    "inn",
    "passport",
    "pinfl",
    "salary",
    "secret",
    "\u0434\u043e\u0433\u043e\u0432\u043e\u0440",
    "\u0437\u0430\u0440\u043f\u043b\u0430\u0442",
    "\u0438\u043d\u043d",
    "\u043f\u0430\u0441\u043f\u043e\u0440\u0442",
    "\u043f\u0438\u043d\u0444\u043b",
    "\u0441\u0435\u043a\u0440\u0435\u0442",
}

SENSITIVE_EXTENSIONS = {
    ".csv",
    ".doc",
    ".docx",
    ".pdf",
    ".rtf",
    ".txt",
    ".xls",
    ".xlsx",
}

WINDOWS_SYSTEM_DRIVES = {"c:"}
LINUX_REMOVABLE_PREFIXES = ("/media/", "/mnt/", "/run/media/")


@dataclass
class EndpointAgentConfig:
    api: str
    agent_id: str
    hostname: str
    username: str
    watch_paths: list[Path]
    state_file: Path
    interval: int = 30
    max_file_size: int = 25 * 1024 * 1024
    max_files: int = 2000
    dry_run: bool = False
    baseline: bool = False
    enforcement_mode: str = "monitor"
    quarantine_dir: Path | None = None
    policy_source: str = "gateway"
    effective_policy_version: str = POLICY_VERSION
    risky_extensions: set[str] = field(default_factory=lambda: set(RISKY_EXTENSIONS))
    sensitive_extensions: set[str] = field(default_factory=lambda: set(SENSITIVE_EXTENSIONS))
    sensitive_name_keywords: set[str] = field(default_factory=lambda: set(SENSITIVE_NAME_KEYWORDS))
    quarantine_event_types: set[str] = field(
        default_factory=lambda: {"endpoint.file.risky_extension", "endpoint.file.removable_copy"}
    )


@dataclass
class FileSnapshot:
    path: str
    size_bytes: int
    mtime: float
    sha256: str

    def as_state(self) -> dict[str, Any]:
        return {"size_bytes": self.size_bytes, "mtime": self.mtime, "sha256": self.sha256}


@dataclass
class AgentState:
    files: dict[str, dict[str, Any]] = field(default_factory=dict)
    emitted_removable_roots: set[str] = field(default_factory=set)

    @classmethod
    def load(cls, path: Path) -> "AgentState":
        if not path.exists():
            return cls()
        data = json.loads(path.read_text(encoding="utf-8"))
        return cls(
            files=data.get("files", {}),
            emitted_removable_roots=set(data.get("emitted_removable_roots", [])),
        )

    def save(self, path: Path) -> None:
        path.parent.mkdir(parents=True, exist_ok=True)
        payload = {
            "files": self.files,
            "emitted_removable_roots": sorted(self.emitted_removable_roots),
            "updated_at": utc_timestamp(),
        }
        path.write_text(json.dumps(payload, indent=2, sort_keys=True), encoding="utf-8")


class EndpointClient:
    def __init__(self, api: str) -> None:
        self.api = api.rstrip("/")

    def post_json(self, path: str, payload: dict[str, Any]) -> dict[str, Any]:
        response = httpx.post(f"{self.api}{path}", json=payload, timeout=15)
        response.raise_for_status()
        return response.json()

    def register(self, config: EndpointAgentConfig) -> dict[str, Any]:
        payload = {
            "agent_id": config.agent_id,
            "hostname": config.hostname,
            "os_type": platform.system().lower() or "unknown",
            "os_version": platform.version(),
            "ip_address": local_ip(),
            "username": config.username,
            "policy_version": agent_policy_version(config),
        }
        return self.post_json("/api/v1/agents/register", payload)

    def heartbeat(self, config: EndpointAgentConfig, status: str = "online") -> dict[str, Any]:
        payload = {
            "hostname": config.hostname,
            "username": config.username,
            "ip_address": local_ip(),
            "status": status,
            "policy_version": agent_policy_version(config),
        }
        return self.post_json(f"/api/v1/agents/{config.agent_id}/heartbeat", payload)

    def emit_event(self, event: dict[str, Any]) -> dict[str, Any]:
        return self.post_json("/api/v1/events/endpoint", event)

    def get_policy(self, agent_id: str) -> dict[str, Any]:
        response = httpx.get(f"{self.api}/api/v1/agents/{agent_id}/policy", timeout=15)
        response.raise_for_status()
        return response.json()


def utc_timestamp() -> str:
    return datetime.now(timezone.utc).isoformat().replace("+00:00", "Z")


def agent_policy_version(config: EndpointAgentConfig) -> str:
    if config.effective_policy_version == POLICY_VERSION:
        return POLICY_VERSION
    return f"{POLICY_VERSION}:{config.effective_policy_version}"


def local_ip() -> str:
    try:
        with socket.socket(socket.AF_INET, socket.SOCK_DGRAM) as sock:
            sock.connect(("8.8.8.8", 80))
            return sock.getsockname()[0]
    except OSError:
        try:
            return socket.gethostbyname(socket.gethostname())
        except OSError:
            return "127.0.0.1"


def safe_path(path: Path) -> str:
    return str(path.resolve())


def quarantine_dir(config: EndpointAgentConfig) -> Path:
    return config.quarantine_dir or config.state_file.parent / "quarantine"


def is_under_path(path: Path, parent: Path) -> bool:
    try:
        return path.resolve().is_relative_to(parent.resolve())
    except OSError:
        return False


def safe_file_name(name: str) -> str:
    cleaned = "".join(char if char.isalnum() or char in "._-" else "_" for char in name)
    return cleaned.strip("._") or "file.bin"


def normalize_extension_set(values: Iterable[Any]) -> set[str]:
    normalized = set()
    for value in values:
        token = str(value).strip().lower()
        if not token:
            continue
        normalized.add(token if token.startswith(".") else f".{token}")
    return normalized


def normalize_string_set(values: Iterable[Any]) -> set[str]:
    return {str(value).strip().lower() for value in values if str(value).strip()}


def apply_endpoint_policy(config: EndpointAgentConfig, policy: dict[str, Any]) -> EndpointAgentConfig:
    mode = str(policy.get("enforcement_mode") or config.enforcement_mode).lower()
    if mode in {"monitor", "enforce"}:
        config.enforcement_mode = mode

    watch_paths = policy.get("watch_paths")
    if isinstance(watch_paths, list) and watch_paths:
        config.watch_paths = [Path(str(path)) for path in watch_paths if str(path).strip()]
    # Keep existing watch paths if policy returns empty list

    quarantine_path = policy.get("quarantine_dir")
    if quarantine_path:
        config.quarantine_dir = Path(str(quarantine_path))

    if policy.get("max_file_size"):
        config.max_file_size = max(1, int(policy["max_file_size"]))
    if policy.get("max_files"):
        config.max_files = max(1, int(policy["max_files"]))

    if isinstance(policy.get("risky_extensions"), list):
        config.risky_extensions = normalize_extension_set(policy["risky_extensions"])
    if isinstance(policy.get("sensitive_extensions"), list):
        config.sensitive_extensions = normalize_extension_set(policy["sensitive_extensions"])
    if isinstance(policy.get("sensitive_name_keywords"), list):
        config.sensitive_name_keywords = normalize_string_set(policy["sensitive_name_keywords"])
    if isinstance(policy.get("quarantine_event_types"), list):
        config.quarantine_event_types = normalize_string_set(policy["quarantine_event_types"])

    config.effective_policy_version = str(policy.get("version") or config.effective_policy_version)
    return config


def is_within_limit(path: Path, max_file_size: int) -> bool:
    try:
        return path.is_file() and path.stat().st_size <= max_file_size
    except OSError:
        return False


def iter_files(watch_paths: Iterable[Path], max_file_size: int, max_files: int) -> Iterable[Path]:
    seen = 0
    for root in watch_paths:
        if not root.exists():
            continue
        if root.is_file() and is_within_limit(root, max_file_size):
            yield root
            seen += 1
            if seen >= max_files:
                return
            continue
        for item in root.rglob("*"):
            if is_within_limit(item, max_file_size):
                yield item
                seen += 1
                if seen >= max_files:
                    return


def enforcement_enabled(config: EndpointAgentConfig) -> bool:
    return config.enforcement_mode.lower() == "enforce"


def quarantine_target(path: Path, snapshot: FileSnapshot, config: EndpointAgentConfig) -> Path:
    return quarantine_dir(config) / f"{snapshot.sha256[:16]}_{safe_file_name(path.name)}"


def quarantine_file(path: Path, snapshot: FileSnapshot, config: EndpointAgentConfig, reason: str) -> dict[str, Any]:
    target = quarantine_target(path, snapshot, config)
    result = {
        "mode": config.enforcement_mode,
        "reason": reason,
        "original_path": snapshot.path,
        "quarantine_path": safe_path(target),
    }
    if config.dry_run:
        return {**result, "status": "dry_run"}
    if is_under_path(path, quarantine_dir(config)):
        return {**result, "status": "already_in_quarantine"}
    if not path.exists():
        return {**result, "status": "missing_source"}

    quarantine_dir(config).mkdir(parents=True, exist_ok=True)
    final_target = target
    suffix = 1
    while final_target.exists():
        final_target = target.with_name(f"{target.stem}_{suffix}{target.suffix}")
        suffix += 1
    shutil.move(str(path), str(final_target))
    return {**result, "status": "quarantined", "quarantine_path": safe_path(final_target)}


def sha256_file(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def snapshot_file(path: Path) -> FileSnapshot | None:
    try:
        stat = path.stat()
        return FileSnapshot(
            path=safe_path(path),
            size_bytes=stat.st_size,
            mtime=stat.st_mtime,
            sha256=sha256_file(path),
        )
    except OSError:
        return None


def is_removable_path(path: Path) -> bool:
    path_text = safe_path(path).replace("\\", "/")
    lower = path_text.lower()
    if len(lower) >= 2 and lower[1] == ":":
        return lower[:2] not in WINDOWS_SYSTEM_DRIVES
    return lower.startswith(LINUX_REMOVABLE_PREFIXES)


def removable_root(path: Path) -> str | None:
    path_text = safe_path(path).replace("\\", "/")
    lower = path_text.lower()
    if len(lower) >= 2 and lower[1] == ":":
        if lower[:2] not in WINDOWS_SYSTEM_DRIVES:
            return lower[:2].upper()
        return None
    for prefix in LINUX_REMOVABLE_PREFIXES:
        if lower.startswith(prefix):
            parts = path_text.strip("/").split("/")
            if len(parts) >= 2 and parts[0] in {"media", "mnt"}:
                return "/" + "/".join(parts[:2])
            if len(parts) >= 3 and parts[0] == "run" and parts[1] == "media":
                return "/" + "/".join(parts[:3])
    return None


def sensitive_name_reasons(path: Path, config: EndpointAgentConfig) -> list[str]:
    name = path.name.lower()
    reasons: list[str] = []
    if path.suffix.lower() in config.sensitive_extensions:
        for keyword in config.sensitive_name_keywords:
            if keyword in name:
                reasons.append(f"name_keyword:{keyword}")
    return reasons


def classify_file(
    path: Path,
    snapshot: FileSnapshot,
    previous: dict[str, Any] | None,
    config: EndpointAgentConfig,
) -> list[dict[str, Any]]:
    events: list[dict[str, Any]] = []
    ext = path.suffix.lower()
    path_text = snapshot.path
    changed = previous is None or previous.get("sha256") != snapshot.sha256
    first_seen = previous is None

    if ext in config.risky_extensions and changed:
        events.append(
            {
                "event_type": "endpoint.file.risky_extension",
                "severity": "high",
                "action": "alert",
                "category": "file_risk",
                "application": "Endpoint Agent",
                "payload": {
                    "path": path_text,
                    "extension": ext,
                    "sha256": snapshot.sha256,
                    "size_bytes": snapshot.size_bytes,
                    "first_seen": first_seen,
                    "reason": "risky_extension",
                },
            }
        )

    sensitive_reasons = sensitive_name_reasons(path, config)
    if sensitive_reasons and changed:
        events.append(
            {
                "event_type": "endpoint.file.sensitive_detected",
                "severity": "medium",
                "action": "alert",
                "category": "sensitive_file",
                "application": "Endpoint Agent",
                "payload": {
                    "path": path_text,
                    "sha256": snapshot.sha256,
                    "size_bytes": snapshot.size_bytes,
                    "first_seen": first_seen,
                    "reasons": sensitive_reasons,
                },
            }
        )

    if is_removable_path(path) and changed:
        events.append(
            {
                "event_type": "endpoint.file.removable_copy",
                "severity": "high" if sensitive_reasons else "medium",
                "action": "alert",
                "category": "exfiltration",
                "application": "Removable Media",
                "payload": {
                    "destination_path": path_text,
                    "sha256": snapshot.sha256,
                    "size_bytes": snapshot.size_bytes,
                    "first_seen": first_seen,
                    "sensitive_reasons": sensitive_reasons,
                },
            }
        )

    return events


def apply_file_enforcement(
    path: Path,
    snapshot: FileSnapshot,
    event: dict[str, Any],
    config: EndpointAgentConfig,
) -> dict[str, Any]:
    if not enforcement_enabled(config):
        return event

    event_type = event["event_type"]
    payload = event.setdefault("payload", {})
    should_quarantine = event_type in config.quarantine_event_types
    if event_type == "endpoint.file.removable_copy" and not payload.get("sensitive_reasons"):
        should_quarantine = False
    if not should_quarantine:
        return event

    enforcement = quarantine_file(path, snapshot, config, event_type)
    event["action"] = "quarantine"
    payload["enforcement"] = enforcement
    return event


def decorate_event(event: dict[str, Any], config: EndpointAgentConfig) -> dict[str, Any]:
    payload = {
        "event_id": str(uuid.uuid4()),
        "timestamp": utc_timestamp(),
        "agent_id": config.agent_id,
        "hostname": config.hostname,
        "username": config.username,
        **event,
    }
    return payload


def removable_root_events(config: EndpointAgentConfig, state: AgentState) -> list[dict[str, Any]]:
    events: list[dict[str, Any]] = []
    for root in config.watch_paths:
        if not root.exists():
            continue
        marker = removable_root(root)
        if marker and marker not in state.emitted_removable_roots:
            state.emitted_removable_roots.add(marker)
            events.append(
                decorate_event(
                    {
                        "event_type": "endpoint.usb.storage_path_seen",
                        "severity": "medium",
                        "action": "alert",
                        "category": "removable_media",
                        "application": "Removable Media",
                        "payload": {"root": marker, "watch_path": safe_path(root)},
                    },
                    config,
                )
            )
    return events


def scan_once(config: EndpointAgentConfig, state: AgentState) -> list[dict[str, Any]]:
    events = [] if config.baseline else removable_root_events(config, state)
    for path in iter_files(config.watch_paths, config.max_file_size, config.max_files):
        if is_under_path(path, quarantine_dir(config)):
            continue
        snapshot = snapshot_file(path)
        if snapshot is None:
            continue
        previous = state.files.get(snapshot.path)
        if not config.baseline:
            for event in classify_file(path, snapshot, previous, config):
                event = apply_file_enforcement(path, snapshot, event, config)
                events.append(decorate_event(event, config))
        state.files[snapshot.path] = snapshot.as_state()
    return events


def emit_events(
    config: EndpointAgentConfig,
    events: list[dict[str, Any]],
    client: EndpointClient | None = None,
) -> list[dict[str, Any]]:
    if config.dry_run:
        for event in events:
            sys.stdout.write(json.dumps(event, ensure_ascii=False, sort_keys=True) + "\n")
        return [{"status": "dry-run", "event": event} for event in events]

    if client is None:
        client = EndpointClient(config.api)
        client.register(config)
        client.heartbeat(config)
    results = []
    for event in events:
        results.append(client.emit_event(event))
    return results


def prepare_runtime_config(config: EndpointAgentConfig) -> tuple[EndpointAgentConfig, EndpointClient | None]:
    if config.dry_run:
        return config, None

    client = EndpointClient(config.api)
    client.register(config)
    if config.policy_source == "gateway":
        policy = client.get_policy(config.agent_id)
        apply_endpoint_policy(config, policy)
    client.heartbeat(config)
    return config, client


def run_once(config: EndpointAgentConfig) -> list[dict[str, Any]]:
    config, client = prepare_runtime_config(config)
    state = AgentState.load(config.state_file)
    events = scan_once(config, state)
    state.save(config.state_file)
    results = emit_events(config, events, client)
    LOGGER.info("scanned_paths=%s events=%s dry_run=%s", len(config.watch_paths), len(events), config.dry_run)
    return results


def run_loop(config: EndpointAgentConfig) -> None:
    config, client = prepare_runtime_config(config)
    state = AgentState.load(config.state_file)
    while True:
        events = scan_once(config, state)
        state.save(config.state_file)
        emit_events(config, events, client)
        LOGGER.info("scanned_paths=%s events=%s dry_run=%s", len(config.watch_paths), len(events), config.dry_run)
        time.sleep(config.interval)


def default_watch_paths() -> list[Path]:
    paths: list[Path] = []
    home = Path.home()
    for folder in ["Desktop", "Documents", "Downloads"]:
        p = home / folder
        if p.exists():
            paths.append(p)
    # Add removable drives only (USB flash drives)
    if platform.system() == "Windows":
        try:
            import ctypes
            DRIVE_REMOVABLE = 2
            DRIVE_FIXED = 3
            for letter in string.ascii_uppercase:
                if letter == "C":
                    continue
                drive_type = ctypes.windll.kernel32.GetDriveTypeW(f"{letter}:\\")
                if drive_type == DRIVE_REMOVABLE:
                    drive = Path(f"{letter}:\\")
                    if drive.exists():
                        paths.append(drive)
        except Exception:
            pass
    if not paths:
        paths.append(home)
    return paths


def parse_args(argv: list[str] | None = None) -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="DLP/DPI endpoint agent MVP")
    parser.add_argument("--api", default=DEFAULT_API)
    parser.add_argument("--agent-id", default=os.getenv("DLPDPI_AGENT_ID") or f"agent-{platform.node() or 'local'}")
    parser.add_argument("--hostname", default=platform.node() or "endpoint-local")
    parser.add_argument("--username", default=os.getenv("USERNAME") or os.getenv("USER") or "local.user")
    parser.add_argument("--state-file", type=Path, default=DEFAULT_STATE_FILE)
    parser.add_argument("--watch-path", action="append", type=Path)
    parser.add_argument("--interval", type=int, default=30)
    parser.add_argument("--max-file-size", type=int, default=25 * 1024 * 1024)
    parser.add_argument("--max-files", type=int, default=2000)
    parser.add_argument(
        "--enforcement-mode",
        choices=["monitor", "enforce"],
        default=DEFAULT_ENFORCEMENT_MODE,
    )
    parser.add_argument(
        "--quarantine-dir",
        type=Path,
        default=Path(DEFAULT_QUARANTINE_DIR) if DEFAULT_QUARANTINE_DIR else None,
    )
    parser.add_argument("--policy-source", choices=["gateway", "local"], default=DEFAULT_POLICY_SOURCE)
    parser.add_argument("--dry-run", action="store_true")
    parser.add_argument("--baseline", action="store_true")
    mode = parser.add_mutually_exclusive_group()
    mode.add_argument("--once", action="store_true")
    mode.add_argument("--loop", action="store_true")
    return parser.parse_args(argv)


def config_from_args(args: argparse.Namespace) -> EndpointAgentConfig:
    return EndpointAgentConfig(
        api=args.api,
        agent_id=args.agent_id,
        hostname=args.hostname,
        username=args.username,
        watch_paths=args.watch_path or default_watch_paths(),
        state_file=args.state_file,
        interval=args.interval,
        max_file_size=args.max_file_size,
        max_files=args.max_files,
        dry_run=args.dry_run,
        baseline=args.baseline,
        enforcement_mode=args.enforcement_mode,
        quarantine_dir=args.quarantine_dir,
        policy_source=args.policy_source,
    )


def main(argv: list[str] | None = None) -> None:
    args = parse_args(argv)
    config = config_from_args(args)
    if args.loop:
        run_loop(config)
    else:
        run_once(config)


if __name__ == "__main__":
    main()
