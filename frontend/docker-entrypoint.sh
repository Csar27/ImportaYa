#!/bin/sh
set -eu

API_URL="${API_URL:-http://localhost:5080/api}"
printf '{"apiUrl":"%s"}\n' "$API_URL" > /usr/share/nginx/html/config.json
