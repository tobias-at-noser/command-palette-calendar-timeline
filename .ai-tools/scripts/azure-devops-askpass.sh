#!/bin/sh

set -eu

pat_file=${AZURE_DEVOPS_PAT_FILE:-/run/secrets/azure_devops_pat}
[ -r "$pat_file" ] || exit 1
pat=$(cat "$pat_file")
[ -n "$pat" ] || exit 1

prompt=$(printf '%s' "${1:-}" | tr '[:upper:]' '[:lower:]')
case "$prompt" in
  *username*) printf '%s\n' 'nosercloud' ;;
  *password*) printf '%s\n' "$pat" ;;
  *) exit 1 ;;
esac
