#!/usr/bin/env python3
import json
import requests

BASE_URL = "http://localhost:8123"

def run_calculation():
    """Runs a single calculation with specific inputs to verify the fix."""
    calc_request = {
        "Inputs": {
            "VehicleSalesPrice": 1000000,
            "DownpaymentValue": 100000,
            "TermMonths": 48,
            "CustomerRatePercent": 3.5,
            "UpfrontCosts": 50000,
            "UpfrontSubsidies": 20000,
            "DownpaymentIsPercent": False,
            "AdditionalFinancedItems": 0,
            "SubdownIsPercent": False,
            "SubdownPercent": 0,
            "SubdownTHB": 0,
            "Product": "HirePurchase",
            "BalloonIsPercent": False,
            "BalloonPercent": 0,
            "BalloonTHB": 0,
            "PaymentMode": "InArrears"
        }
    }

    try:
        url = f"{BASE_URL}/api/v1/calculate/simple"
        resp = requests.post(url, json=calc_request)

        if resp.status_code != 200:
            print(f"FAIL: POST {url} returned {resp.status_code}")
            print(f"   Response: {resp.text[:500]}")
            return

        result = resp.json()
        
        idc_impact_bps = result.get('UpfrontCostRateImpactBps', 0)
        subsidy_impact_bps = result.get('UpfrontIncomeRateImpactBps', 0)
        
        print("Calculation successful.")
        print(f"  IDC Impact: {idc_impact_bps / 100:.2f}%")
        print(f"  Subsidy Impact: {subsidy_impact_bps / 100:.2f}%")

    except Exception as e:
        print(f"ERROR: Calculation failed: {e}")

if __name__ == "__main__":
    run_calculation()