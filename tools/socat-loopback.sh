#!/usr/bin/env bash
#
# socat-loopback.sh — macOS/Linux virtual serial port pair for the Delfin BLE simulator.
#
# Creates a PTY pair linked to two stable paths so you don't have to chase the
# dynamic /dev/ttysNNN names socat prints. This is the macOS/Linux equivalent of
# com0com on Windows.
#
# Usage:
#   ./tools/socat-loopback.sh
#
# It creates:
#   /tmp/delfin_sim   <-- run the simulator here
#   /tmp/delfin_app   <-- point the driver/demo here
#
# Then, in two more terminals:
#   dotnet tools/Delfin.Ble.SimulatorDemo.dll /tmp/delfin_sim DCC1423      (release kit)
#   dotnet run --project src/Delfin.Ble.Demo -- /tmp/delfin_app DCC1423
#
# Press Ctrl+C here to tear down the pair.

set -euo pipefail

if ! command -v socat >/dev/null 2>&1; then
  echo "socat is not installed."
  echo "Install it with: brew install socat   (macOS)"
  echo "             or: sudo apt install socat (Linux)"
  exit 1
fi

SIM_LINK=/tmp/delfin_sim
APP_LINK=/tmp/delfin_app

echo "Creating virtual serial port pair. socat prints the real PTY devices below"
echo "(for example /dev/ttys000 and /dev/ttys001; the numbers differ per run)."
echo "These symlinks point to them and can be used instead:"
echo "  Simulator side:  $SIM_LINK  (first PTY printed by socat)"
echo "  App/driver side: $APP_LINK  (second PTY printed by socat)"
echo
echo "Leave this running. In separate terminals:"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
if [ -f "$SCRIPT_DIR/Delfin.Ble.SimulatorDemo.dll" ]; then
  echo "  dotnet tools/Delfin.Ble.SimulatorDemo.dll $SIM_LINK DCC1423"
else
  echo "  dotnet run --project src/Delfin.Ble.SimulatorDemo -- $SIM_LINK DCC1423"
fi
echo "  dotnet run --project src/Delfin.Ble.Demo -- $APP_LINK DCC1423"
echo
echo "Press Ctrl+C to stop."
echo

exec socat -d -d \
  pty,raw,echo=0,link="$SIM_LINK" \
  pty,raw,echo=0,link="$APP_LINK"
