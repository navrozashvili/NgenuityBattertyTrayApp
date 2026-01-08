"""
HyperX Cloud III S Wireless (dongle) - battery query (experimental HID).

This is a small, shareable research script:
  - It sends a vendor-specific HID Output report to request battery status
  - It listens for the corresponding HID Input report and prints battery %
  - It also listens for a separate "charging state" report, if present

Battery protocol (best-effort, reverse engineered):
  - Host -> Device (HID SET_REPORT / Output, ReportID 0x0C, 64 bytes):
      0C 02 03 01 00 06 00 ...
  - Device -> Host (interrupt IN, ReportID 0x0C):
      0C 02 03 01 00 06 <PCT> <FLAGS1> <FLAGS2> ...
    where <PCT> is battery percent (0-100). The meaning of FLAGS is unknown.

Charging detection (best-effort, reverse engineered):
  - Some devices emit a separate ReportID 0x0D message:
      0D 02 03 00 0A <STATE> ...
    where <STATE> has been observed as 00/01.

IMPORTANT behavioral note:
  - On some firmware, the 0x0D "charging" report is EVENT-STYLE (edge-triggered),
    not periodic telemetry.
  - That means you may only see <STATE>=01 briefly right after plugging in,
    and <STATE>=00 briefly right after unplugging.
  - In between those transitions, there may be no charging messages at all.

This script therefore reports charging as:
  - yes/no only if a charging-state report was seen "recently"
  - unknown otherwise

Install:
  python -m pip install hidapi

Usage:
  python hyperx_cloud3s_battery.py
  python hyperx_cloud3s_battery.py --watch

Safety:
  Use only on hardware/software you're authorized to test.
"""

from __future__ import annotations

import argparse
import sys
import time
from typing import Any, Dict, List, Optional, Tuple


VID_DEFAULT = 0x03F0
PID_DEFAULT = 0x06BE
REPORT_ID = 0x0C
CMD_BATTERY = 0x06
CHG_REPORT_ID = 0x0D
CHG_TAG = 0x0A


def _import_hid():
    try:
        import hid  # type: ignore

        return hid
    except Exception as e:  # pragma: no cover
        raise RuntimeError(
            "Could not import 'hid'. Try:\n"
            "  python -m pip install hidapi\n"
        ) from e


def _pad_to(length: int, buf: bytes) -> bytes:
    if length <= 0 or len(buf) == length:
        return buf
    if len(buf) > length:
        raise ValueError(f"Report too long: {len(buf)} > {length}")
    return buf + b"\x00" * (length - len(buf))


def enumerate_devices(hid_mod, vid: int, pid: int) -> List[Dict[str, Any]]:
    return list(hid_mod.enumerate(vid, pid))


def pick_best_path(devs: List[Dict[str, Any]]) -> Optional[bytes]:
    # Heuristic: choose the interface with the largest max_output_report_length.
    best = None
    best_out = -1
    for d in devs:
        out_len = int(d.get("max_output_report_length") or 0)
        if out_len > best_out:
            best_out = out_len
            best = d
    if not best:
        return None
    return best["path"]


def open_device(
    vid: int, pid: int, path: Optional[bytes]
) -> Tuple[Any, Any, bytes, List[Dict[str, Any]]]:
    hid = _import_hid()
    devs = enumerate_devices(hid, vid, pid)
    if not devs:
        raise RuntimeError(f"No matching HID devices found for {vid:04X}:{pid:04X}.")

    if path is None:
        best = pick_best_path(devs)
        if best is None:
            raise RuntimeError("Could not auto-pick a device path; pass --path.")
        path = best

    h = hid.device()
    h.open_path(path)
    return hid, h, path, devs


def build_battery_query(out_len: int) -> bytes:
    # ReportID + fixed header + battery command
    req = bytes([REPORT_ID, 0x02, 0x03, 0x01, 0x00, CMD_BATTERY, 0x00])
    return _pad_to(out_len or 64, req)


def parse_battery_reply(buf: bytes) -> Optional[Tuple[int, int, int]]:
    # Expect: 0C 02 03 01 00 06 <PCT> <FLAGS1> <FLAGS2> ...
    if len(buf) < 9:
        return None
    if buf[0] != REPORT_ID:
        return None
    if buf[5] != CMD_BATTERY:
        return None
    pct = int(buf[6])
    flags1 = int(buf[7])
    flags2 = int(buf[8])
    return pct, flags1, flags2


def parse_charging_state(buf: bytes) -> Optional[bool]:
    """
    Best-effort charging detection based on charge.pcapng.

    Expected prefix:
      0D 02 03 00 0A <STATE> ...

    Returns:
      True  -> charging/plugged
      False -> not charging/unplugged
      None  -> not a recognized charging-status report
    """
    if len(buf) < 6:
        return None
    if buf[0] != CHG_REPORT_ID:
        return None
    if buf[1] != 0x02 or buf[2] != 0x03 or buf[3] != 0x00:
        return None
    if buf[4] != CHG_TAG:
        return None
    state = int(buf[5])
    if state == 1:
        return True
    if state == 0:
        return False
    return None


def _charging_display(
    last_value: Optional[bool],
    last_seen_ts: Optional[float],
    stale_ms: int,
) -> str:
    """
    Convert last seen charging EVENT into a human-friendly status.

    Because this device may only emit charging state on transitions, we treat it
    as "unknown" when the last event is older than stale_ms.
    """
    if last_value is None or last_seen_ts is None:
        return "unknown"
    age_ms = int((time.time() - last_seen_ts) * 1000)
    if age_ms > stale_ms:
        return "unknown"
    return "yes" if last_value else "no"


def query_battery(
    h,
    in_len: int,
    out_len: int,
    timeout_ms: int,
    verbose: bool,
    *,
    charging_last_value: Optional[bool],
    charging_last_seen_ts: Optional[float],
    charging_stale_ms: int,
) -> Tuple[int, Optional[bool], Optional[float]]:
    req = build_battery_query(out_len or 64)
    h.set_nonblocking(True)
    h.write(list(req))

    deadline = time.time() + (timeout_ms / 1000.0)
    last_charging = charging_last_value
    last_charging_seen = charging_last_seen_ts
    got_battery: Optional[Tuple[int, int, int]] = None
    while time.time() < deadline:
        data = h.read(in_len or 64)
        if not data:
            time.sleep(0.01)
            continue

        b = bytes(data)

        chg = parse_charging_state(b)
        if chg is not None:
            last_charging = chg
            last_charging_seen = time.time()
            if verbose:
                print(f"Charging state report: {'YES' if chg else 'NO'}")
            # Keep reading; we still want the battery reply in this call.
            continue

        parsed = parse_battery_reply(b)
        if parsed is not None:
            got_battery = parsed
            break

        if verbose:
            print(f"Unexpected report: {b.hex('-').upper()}")

    if got_battery is None:
        print(f"No battery reply within {timeout_ms}ms", file=sys.stderr)
        return 2, last_charging, last_charging_seen

    pct, flags1, flags2 = got_battery
    charging_str = _charging_display(last_charging, last_charging_seen, charging_stale_ms)

    print(f"Battery: {pct}%  (charging: {charging_str}, flags: {flags1:02X} {flags2:02X})")
    return 0, last_charging, last_charging_seen




def main(argv: List[str]) -> int:
    p = argparse.ArgumentParser()
    p.add_argument("--vid", type=lambda s: int(s, 0), default=VID_DEFAULT)
    p.add_argument("--pid", type=lambda s: int(s, 0), default=PID_DEFAULT)
    p.add_argument("--path", type=lambda s: s.encode("utf-8"), default=None)
    p.add_argument("--timeout-ms", type=int, default=1000)
    p.add_argument("--watch", action="store_true", help="Poll repeatedly.")
    p.add_argument("--interval-ms", type=int, default=2000, help="Used with --watch.")
    p.add_argument(
        "--charging-stale-ms",
        type=int,
        default=2500,
        help=(
            "Charging is treated as unknown if no charging-state report was seen "
            "within this window. This matches devices that emit charging only on "
            "plug/unplug transitions."
        ),
    )
    p.add_argument("--verbose", action="store_true")
    args = p.parse_args(argv)

    _, h, path, devs = open_device(args.vid, args.pid, args.path)
    try:
        in_len = 0
        out_len = 0
        for d in devs:
            if d.get("path") == path:
                in_len = int(d.get("max_input_report_length") or 0)
                out_len = int(d.get("max_output_report_length") or 0)
                break

        last_chg: Optional[bool] = None
        last_chg_seen: Optional[float] = None

        if not args.watch:
            rc, _, _ = query_battery(
                h,
                in_len,
                out_len,
                args.timeout_ms,
                args.verbose,
                charging_last_value=last_chg,
                charging_last_seen_ts=last_chg_seen,
                charging_stale_ms=args.charging_stale_ms,
            )
            return rc

        while True:
            rc, last_chg, last_chg_seen = query_battery(
                h,
                in_len,
                out_len,
                args.timeout_ms,
                args.verbose,
                charging_last_value=last_chg,
                charging_last_seen_ts=last_chg_seen,
                charging_stale_ms=args.charging_stale_ms,
            )
            if rc != 0 and args.verbose:
                print("(retrying...)")
            time.sleep(max(0.1, args.interval_ms / 1000.0))
    finally:
        try:
            h.close()
        except Exception:
            pass


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))


