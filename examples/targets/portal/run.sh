#!/bin/zsh

set -euo pipefail

SCRIPT_DIR=${0:A:h}
IDF_EXPORT="/Users/ralfhille/esp/v5.5.3/esp-idf/export.sh"

if [[ ! -f "$IDF_EXPORT" ]]; then
  echo "ESP-IDF export script nicht gefunden: $IDF_EXPORT" >&2
  exit 1
fi

source "$IDF_EXPORT"
cd "$SCRIPT_DIR"

if [[ $# -eq 0 ]]; then
  set -- build
fi

case "$1" in
  build)
    shift
    exec idf.py build "$@"
    ;;
  flash)
    shift
    exec idf.py flash "$@"
    ;;
  monitor)
    shift
    exec idf.py monitor "$@"
    ;;
  clean)
    shift
    exec idf.py fullclean "$@"
    ;;
  *)
    exec idf.py "$@"
    ;;
esac
