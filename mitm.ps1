# This script starts mitmproxy with the acli_reasoning_injector.py script.
# It uses $PSScriptRoot to ensure it can be run from any directory,
# as long as acli_reasoning_injector.py is in the same directory as this script.

Write-Host "Starting mitmproxy with acli_reasoning_injector.py..."
mitmproxy -s "C:\Users\PATKRAN\Python\07_controlling\financial_calculator\acli_reasoning_injector.py" --set ssl_insecure=true