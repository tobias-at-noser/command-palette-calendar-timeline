#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
cd "$SCRIPT_DIR"

COMPOSE_FILES=(-f compose-opencode.yml -f compose-opencode.override.yml)
if [[ -f user.txt && -f password.txt ]]; then
  COMPOSE_FILES+=(-f compose-opencode.credentials.override.yml)
  echo "Local website credentials enabled."
else
  echo "Local website credentials disabled."
fi

REPO_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"
OPENCODE_WORKTREES_PATH="${REPO_DIR}.worktrees"
export OPENCODE_WORKTREES_PATH
echo "OpenCode Worktrees Path $OPENCODE_WORKTREES_PATH"
mkdir -p "$OPENCODE_WORKTREES_PATH"

if command -v podman >/dev/null 2>&1; then
  COMPOSE_CMD=(podman compose)
elif command -v docker >/dev/null 2>&1; then
  COMPOSE_CMD=(docker compose)
elif command -v docker-compose >/dev/null 2>&1; then
  COMPOSE_CMD=(docker-compose)
elif command -v docker-compose.exe >/dev/null 2>&1; then
  COMPOSE_CMD=(docker-compose.exe)
else
  echo "Error: neither podman nor docker compose is available in PATH." >&2
  exit 1
fi

re='^[0-9]+\.[0-9]+(\.[0-9]+)?([\-\.][0-9A-Za-z][0-9A-Za-z.-]*)?$'

if command -v podman >/dev/null 2>&1; then
  IMAGE_CMD=(podman)
elif command -v docker >/dev/null 2>&1; then
  IMAGE_CMD=(docker)
else
  IMAGE_CMD=()
fi

valid_tag() {
  [ -n "${1:-}" ] && printf '%s' "$1" | grep -Eq "$re"
}

github_latest_tag() {
  curl -fsSL -H 'Accept: application/vnd.github+json' --retry 3 --retry-delay 1 \
    "https://api.github.com/repos/$1/releases/latest" 2>/dev/null \
    | grep '"tag_name":' \
    | sed -E 's/.*"v?([^"]+)".*/\1/' \
    || true
}

local_latest_tag() {
  [ "${#IMAGE_CMD[@]}" -gt 0 ] || return 0
  "${IMAGE_CMD[@]}" image ls "$1" --format '{{.Tag}}' 2>/dev/null \
    | grep -Ev '^(latest|<none>|)$' \
    | grep -E "$re" \
    | sort -Vr \
    | head -n 1 \
    || true
}

resolve_tag() {
  var_name="$1"
  repo="$2"
  image="$3"

  preset="${!var_name:-}"
  if valid_tag "$preset"; then
    printf '%s' "$preset"
    return 0
  fi

  tag="$(github_latest_tag "$repo")"
  if valid_tag "$tag"; then
    printf '%s' "$tag"
    return 0
  fi

  tag="$(local_latest_tag "$image")"
  if valid_tag "$tag"; then
    echo "Warning: GitHub latest release unavailable; using local $image:$tag" >&2
    printf '%s' "$tag"
    return 0
  fi

  echo "Error: could not resolve $var_name from env, GitHub, or local image $image" >&2
  return 1
}

compose_image_base() {
  service="$1"
  var_name="$2"
  marker="__tag_probe__"

  env "$var_name=$marker" "${COMPOSE_CMD[@]}" "${COMPOSE_FILES[@]}" config \
    | awk -v svc="$service" -v marker=":$marker" '
      $1 == svc ":" { in_service=1; next }
      in_service && /^  [A-Za-z0-9_-]+:$/ { in_service=0 }
      in_service && $1 == "-" && index($2, marker) {
        sub(/^"/, "", $2)
        sub(/"$/, "", $2)
        sub(marker "$", "", $2)
        print $2
        exit
      }
    '
}

OPENCHAMBER_IMAGE="$(compose_image_base openchamber OPENCHAMBER_TAG)"
OPENCODE_IMAGE="$(compose_image_base opencode OPENCODE_TAG)"

OPENCHAMBER_TAG="$(resolve_tag OPENCHAMBER_TAG openchamber/openchamber "$OPENCHAMBER_IMAGE")"
OPENCODE_TAG="$(resolve_tag OPENCODE_TAG anomalyco/opencode "$OPENCODE_IMAGE")"

export OPENCHAMBER_TAG
export OPENCODE_TAG
echo "OpenChamber $OPENCHAMBER_TAG"
echo "OpenCode $OPENCODE_TAG"

echo "building..."
"${COMPOSE_CMD[@]}" "${COMPOSE_FILES[@]}" build --pull
echo "up..."
"${COMPOSE_CMD[@]}" "${COMPOSE_FILES[@]}" up -d --remove-orphans
echo "done."
