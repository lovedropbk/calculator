#!/usr/bin/env python3
"""
Comprehensive API test suite to detect type mismatches and data issues
"""
import json
import requests
from typing import Dict, Any, List
import sys

BASE_URL = "http://localhost:8123"

def test_endpoint(method: str, path: str, data: Dict[str, Any] = None) -> Dict[str, Any]:
    """Test an API endpoint and return the response"""
    url = f"{BASE_URL}{path}"
    try:
        if method == "GET":
            resp = requests.get(url)
        elif method == "POST":
            resp = requests.post(url, json=data)
        else:
            raise ValueError(f"Unsupported method: {method}")
        
        if resp.status_code != 200:
            print(f"FAIL: {method} {path} returned {resp.status_code}")
            print(f"   Response: {resp.text[:500]}")
            return None
        
        result = resp.json()
        print(f"OK: {method} {path}")
        return result
    except Exception as e:
        print(f"ERROR: {method} {path} failed: {e}")
        return None

def validate_types(data: Any, path: str = "") -> List[str]:
    """Recursively validate data types and find issues"""
    issues = []
    
    if isinstance(data, dict):
        for key, value in data.items():
            new_path = f"{path}.{key}" if path else key
            
            # Check for string numbers that should be numeric
            if isinstance(value, str):
                try:
                    # Check if it looks like a number
                    if '.' in value or value.replace('-', '').replace('+', '').isdigit():
                        float_val = float(value)
                        if value != "0" and float_val != 0:  # Ignore zero values
                            issues.append(f"String number at {new_path}: '{value}' should be {float_val}")
                except:
                    pass
            
            # Recurse
            issues.extend(validate_types(value, new_path))
    
    elif isinstance(data, list):
        for i, item in enumerate(data):
            issues.extend(validate_types(item, f"{path}[{i}]"))
    
    return issues

def test_calculations():
    """Test calculation endpoints with various inputs"""
    print("\n=== Testing Calculation Endpoints ===")
    
    # Get campaign catalog first
    catalog = test_endpoint("GET", "/api/v1/campaigns/catalog")
    if not catalog:
        print("Failed to get campaign catalog")
        return
    
    print(f"Found {len(catalog)} campaigns in catalog")
    
    # Test standard calculation
    calc_request = {
        "deal": {
            "market": "TH",
            "currency": "THB",
            "product": "HP",
            "price_ex_tax": 1000000,
            "down_payment_amount": 100000,
            "down_payment_percent": 0.1,
            "down_payment_locked": "amount",
            "financed_amount": 900000,
            "term_months": 48,
            "balloon_percent": 0,
            "balloon_amount": 0,
            "timing": "arrears",
            "rate_mode": "fixed_rate",
            "customer_nominal_rate": 0.035,
            "target_installment": 0
        },
        "campaigns": [],
        "idc_items": [],
        "options": {"derive_idc_from_cf": True}
    }
    
    result = test_endpoint("POST", "/api/v1/calculate", calc_request)
    if result:
        # Check for type issues
        issues = validate_types(result)
        if issues:
            print("\nWARNING: Type issues found in calculation response:")
            for issue in issues[:10]:  # Show first 10
                print(f"   - {issue}")
        
        # Check cashflow
        if "cashflow" in result and "schedule" in result["cashflow"]:
            schedule = result["cashflow"]["schedule"]
            print(f"   Cashflow schedule has {len(schedule)} periods")
            if len(schedule) == 0:
                print("   ERROR: Cashflow schedule is empty!")
        else:
            print("   ERROR: No cashflow schedule in response!")

def test_campaign_summaries():
    """Test campaign summaries endpoint"""
    print("\n=== Testing Campaign Summaries ===")
    
    # Get catalog
    catalog = test_endpoint("GET", "/api/v1/campaigns/catalog")
    if not catalog:
        return
    
    # Convert catalog items to campaign DTOs
    campaigns = []
    for item in catalog[:5]:  # Test with first 5 campaigns
        campaign = {
            "id": item.get("id", ""),
            "type": item.get("type", ""),
            "funder": item.get("funder"),
            "description": item.get("description"),
            "parameters": item.get("parameters", {})
        }
        campaigns.append(campaign)
    
    # Test summaries request
    summaries_request = {
        "deal": {
            "market": "TH",
            "currency": "THB",
            "product": "HP",
            "price_ex_tax": 1000000,
            "down_payment_amount": 100000,
            "down_payment_percent": 0.1,
            "down_payment_locked": "amount",
            "financed_amount": 900000,
            "term_months": 48,
            "balloon_percent": 0,
            "balloon_amount": 0,
            "timing": "arrears",
            "rate_mode": "fixed_rate",
            "customer_nominal_rate": 0.035,
            "target_installment": 0
        },
        "state": {
            "dealerCommission": {"mode": "auto"},
            "idcOther": {"value": 0, "userEdited": False},
            "budgetTHB": 50000
        },
        "campaigns": campaigns
    }
    
    result = test_endpoint("POST", "/api/v1/campaigns/summaries", summaries_request)
    if result:
        print(f"   Received {len(result)} campaign summaries")
        if len(result) == 0:
            print("   ERROR: No summaries returned despite sending campaigns!")
        else:
            # Check for type issues
            issues = validate_types(result)
            if issues:
                print("\nWARNING: Type issues found in summaries response:")
                for issue in issues[:10]:
                    print(f"   - {issue}")

def test_with_campaigns():
    """Test calculation with campaigns applied"""
    print("\n=== Testing Calculation with Campaigns ===")
    
    # Get catalog
    catalog = test_endpoint("GET", "/api/v1/campaigns/catalog")
    if not catalog:
        return
    
    # Find a subdown campaign
    subdown = next((c for c in catalog if c.get("type") == "subdown"), None)
    if subdown:
        print(f"   Using campaign: {subdown.get('id')}")
        
        calc_request = {
            "deal": {
                "market": "TH",
                "currency": "THB",
                "product": "HP",
                "price_ex_tax": 1000000,
                "down_payment_amount": 100000,
                "down_payment_percent": 0.1,
                "down_payment_locked": "amount",
                "financed_amount": 900000,
                "term_months": 48,
                "balloon_percent": 0,
                "balloon_amount": 0,
                "timing": "arrears",
                "rate_mode": "fixed_rate",
                "customer_nominal_rate": 0.035,
                "target_installment": 0
            },
            "campaigns": [{
                "id": subdown.get("id"),
                "type": subdown.get("type"),
                "parameters": subdown.get("parameters", {})
            }],
            "idc_items": [],
            "options": {"derive_idc_from_cf": True}
        }
        
        result = test_endpoint("POST", "/api/v1/calculate", calc_request)
        if result:
            # Check cashflow
            if "cashflow" in result and "schedule" in result["cashflow"]:
                schedule = result["cashflow"]["schedule"]
                print(f"   Cashflow schedule has {len(schedule)} periods")
                if len(schedule) > 0:
                    # Check first period
                    first = schedule[0]
                    print(f"   First period: principal={first.get('principal')}, interest={first.get('interest')}, cashflow={first.get('cashflow')}")
            
            # Check campaign audit
            if "quote" in result and "campaign_audit" in result["quote"]:
                audit = result["quote"]["campaign_audit"]
                print(f"   Campaign audit has {len(audit)} entries")
                for entry in audit:
                    print(f"     - {entry.get('campaign_id')}: applied={entry.get('applied')}, impact={entry.get('impact')}")

def main():
    """Run all tests"""
    print("Starting comprehensive API testing...")
    print(f"   Testing against: {BASE_URL}")
    
    # Check health
    health = test_endpoint("GET", "/healthz")
    if not health:
        print("Backend is not healthy! Please start it first.")
        sys.exit(1)
    
    # Run tests
    test_calculations()
    test_campaign_summaries()
    test_with_campaigns()
    
    print("\nTesting complete!")

if __name__ == "__main__":
    main()