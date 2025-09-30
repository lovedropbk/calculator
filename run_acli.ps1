# run-acli-with-reasoning-injection.ps1

# This script configures your environment to use a local mitmproxy instance
# to bypass corporate TLS interception and inject the 'reasoning_effort' parameter.

# --- INSTRUCTIONS ---
# 1. Make sure you have mitmproxy installed (`pip install mitmproxy`).
# 2. Open a new terminal.
# 3. Run 'mitmproxy -s acli_reasoning_injector.py --set ssl_insecure=true' in that terminal and leave it running.
#    (This will also generate the required CA certificate on first run).
# 4. Open another terminal in your project directory.
# 5. Run this script: '.\run-acli-with-reasoning-injection.ps1'
# ---

Write-Host "Configuring environment for local proxy and reasoning injection..."

# Set proxy environment variables to point to mitmproxy (default port 8080)
$env:HTTPS_PROXY="http://127.0.0.1:8080"
$env:HTTP_PROXY="http://127.0.0.1:8080"

# Set the SSL_CERT_FILE to trust the mitmproxy CA certificate.
# mitmproxy automatically generates this in your user profile.
$mitmproxy_ca_path = "$HOME\.mitmproxy\mitmproxy-ca-cert.cer"
$env:SSL_CERT_FILE=$mitmproxy_ca_path

# Also set REQUESTS_CA_BUNDLE for Python's `requests` library
$env:REQUESTS_CA_BUNDLE=$mitmproxy_ca_path
Write-Host "REQUESTS_CA_BUNDLE will be set to: $mitmproxy_ca_path"

Write-Host "SSL_CERT_FILE will be set to: $mitmproxy_ca_path"

# Check if the mitmproxy CA file exists
if (-not (Test-Path $mitmproxy_ca_path)) {
    Write-Error "Mitmproxy CA certificate not found at '$mitmproxy_ca_path'. Please start mitmproxy at least once to generate it."
    exit 1
}

Write-Host "Environment configured. Running 'acli rovodev run'..."
Write-Host "----------------------------------------------------"

# Run the acli command with the configured environment
# Any arguments passed to this script will be forwarded to the acli command.
acli rovodev run $args