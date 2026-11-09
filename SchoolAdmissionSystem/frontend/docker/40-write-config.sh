
set -e
cat > /usr/share/nginx/html/config.js <<JS
window.__APP_CONFIG__ = { apiBaseUrl: "${API_BASE_URL:-http://localhost:8080}" };
JS
