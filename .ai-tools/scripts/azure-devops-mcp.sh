#!/bin/sh

set -eu

pat_file=${AZURE_DEVOPS_PAT_FILE:-/run/secrets/azure_devops_pat}
[ -r "$pat_file" ] || exit 1
pat=$(cat "$pat_file")
[ -n "$pat" ] || exit 1

PERSONAL_ACCESS_TOKEN="$(printf '%s:%s' 'nosercloud' "$pat" | base64 | tr -d '\n')"
export PERSONAL_ACCESS_TOKEN

exec npx -y -p @azure-devops/mcp -p open mcp-server-azuredevops nosercloud \
  --authentication pat \
  -d core repositories search
