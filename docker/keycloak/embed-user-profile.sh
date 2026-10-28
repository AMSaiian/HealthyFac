#!/usr/bin/env bash
# Embeds docker/keycloak/user-profile.json into the realm file as the string that Keycloak expects in
# components["org.keycloak.userprofile.UserProfileProvider"][0].config["kc.user.profile.config"][0].
# Run it after each edit of user-profile.json. The test UserProfile_InRealmFile_EqualsSourceFile fails when the
# two files differ. The script needs jq, or Docker (it then runs the pinned jq image).
set -euo pipefail

JQ_IMAGE="ghcr.io/jqlang/jq:1.7.1"
PROFILE_FILE="user-profile.json"
REALM_FILE="import/healthyfac-realm.json"
PROFILE_PATH='.components["org.keycloak.userprofile.UserProfileProvider"][0].config["kc.user.profile.config"][0]'

cd "$(dirname "${BASH_SOURCE[0]}")"

# Passes the files through standard input, so the Docker path needs no volume mount.
run_jq() {
  if command -v jq > /dev/null 2>&1; then
    jq "$@"
  else
    MSYS_NO_PATHCONV=1 docker run --rm -i "$JQ_IMAGE" "$@"
  fi
}

# A native jq.exe on Windows writes CR LF, and command substitution removes only the LF.
profile="$(run_jq -c . < "$PROFILE_FILE" | tr -d '\r')"

temp_file="$(mktemp "$REALM_FILE.XXXXXX")"
trap 'rm -f "$temp_file"' EXIT

# The files of this repository use CR LF until the line ending cleanup.
run_jq --indent 2 --arg profile "$profile" "$PROFILE_PATH = \$profile" < "$REALM_FILE" \
  | sed 's/\r$//' \
  | awk '{ printf "%s\r\n", $0 }' > "$temp_file"

if cmp -s "$temp_file" "$REALM_FILE"; then
  echo "The realm file was already up to date."
else
  # mktemp creates the file with mode 600, and the Keycloak container must read the realm file.
  chmod 644 "$temp_file"
  mv "$temp_file" "$REALM_FILE"
  echo "The realm file is updated."
fi
