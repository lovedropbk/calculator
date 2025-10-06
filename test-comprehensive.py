#!/usr/bin/env python3
"""
Comprehensive end-to-end test suite for Financial Calculator
Tests all API endpoints, data flows, and validates responses
"""
import json
import requests
import sys
import time
from typing import Dict, Any, List, Optional

BASE_URL = "http://localhost:8123"

class APITester:
    def __init__(self, base_url: str = BASE_URL):
        self.base_url = base_url
        self.session = requests.Session()
        self.errors = []
        self.warnings = []
        
    def test_endpoint(self, method: str, path: str, data: Dict[str, Any] = None, expected_status: int = 200) -> Optional[Dict[str, Any]]:
        """Test an API endpoint and return the response"""
        url = f"{self.base_url}{path}"
        try:
            if method == "GET":
                resp = self.session.get(url, timeout=10)
            elif method == "POST":
                resp = self.session.post(url, json=data, timeout=10)
            else:
                raise ValueError(f"Unsupported method: {method}")
            
            if resp.status_code != expected_status:
                self.errors.append(f"{method} {path} returned {resp.status_code}, expected {expected_status}")
                print(f"FAIL: {method} {path} - Status {resp.status_code}")
                return None
            
            print(f"OK: {method} {path} - Status {resp.status_code}")
            
            if resp.content:
                return resp.json()
            return {}
            
        except Exception as e:
            self.errors.append(f"{method} {path} failed: {e}")
            print(f"ERROR: {method} {path} - {e}")
            return None
    
    def validate_number_field(self, data: Dict, path: str, field: str) -> bool:
        """Validate that a field contains a numeric value"""
        try:
            value = data
            for key in path.split('.'):
                if key:
                    value = value[key]
            
            field_value = value.get(field)
            if field_value is None:
                self.errors.append(f"Missing field: {path}.{field}")
                return False
                
            # Check if it's a number or can be converted
            if isinstance(field_value, (int, float)):
                return True
            elif isinstance(field_value, str):
                try:
                    float(field_value)
                    self.warnings.append(f"String number at {path}.{field}: '{field_value}'")
                    return True
                except:
                    self.errors.append(f"Non-numeric value at {path}.{field}: '{field_value}'")
                    return False
            else:
                self.errors.append(f"Invalid type at {path}.{field}: {type(field_value)}")
                return False
                
        except Exception as e:
            self.errors.append(f"Error accessing {path}.{field}: {e}")
            return False
    
    def test_health(self):
        """Test health endpoint"""
        print("\n=== Testing Health Check ===")
        result = self.test_endpoint("GET", "/healthz")
        return result is not None
    
    def test_parameters(self):
        """Test parameters endpoint"""
        print("\n=== Testing Parameters ===")
        result = self.test_endpoint("GET", "/api/v1/parameters/current")
        if result:
            # Validate structure
            if "parameter_set" not in result:
                self.errors.append("Missing parameter_set in response")
            if "engine_parameter_set" not in result:
                self.errors.append("Missing engine_parameter_set in response")
            print(f"   Parameter set ID: {result.get('parameter_set', {}).get('id', 'N/A')}")
        return result
    
    def test_campaign_catalog(self):
        """Test campaign catalog endpoint"""
        print("\n=== Testing Campaign Catalog ===")
        catalog = self.test_endpoint("GET", "/api/v1/campaigns/catalog")
        if catalog:
            print(f"   Found {len(catalog)} campaigns")
            # Check structure of first campaign
            if len(catalog) > 0:
                first = catalog[0]
                required_fields = ["id", "type", "parameters"]
                for field in required_fields:
                    if field not in first:
                        self.errors.append(f"Campaign missing field: {field}")
                
                # Check known campaign types
                types = set(c.get("type") for c in catalog)
                print(f"   Campaign types: {', '.join(sorted(types))}")
        return catalog
    
    def test_calculation_basic(self):
        """Test basic calculation without campaigns"""
        print("\n=== Testing Basic Calculation ===")
        
        request = {
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
        
        result = self.test_endpoint("POST", "/api/v1/calculate", request)
        if result:
            # Validate quote fields
            if "quote" in result:
                quote = result["quote"]
                self.validate_number_field(result, "quote", "monthly_installment")
                self.validate_number_field(result, "quote", "customer_rate_nominal")
                self.validate_number_field(result, "quote", "customer_rate_effective")
                
                # Check schedule
                if "schedule" in quote and len(quote["schedule"]) > 0:
                    print(f"   Schedule has {len(quote['schedule'])} periods")
                    first = quote["schedule"][0]
                    self.validate_number_field({"first": first}, "first", "principal")
                    self.validate_number_field({"first": first}, "first", "interest")
                    self.validate_number_field({"first": first}, "first", "balance")
                else:
                    self.errors.append("No schedule in calculation response")
            else:
                self.errors.append("No quote in calculation response")
        return result
    
    def test_calculation_with_campaign(self, catalog):
        """Test calculation with a campaign applied"""
        print("\n=== Testing Calculation with Campaign ===")
        
        if not catalog or len(catalog) == 0:
            self.warnings.append("No campaigns available to test")
            return None
        
        # Find a subdown campaign
        subdown = next((c for c in catalog if c.get("type") == "subdown"), catalog[0])
        print(f"   Using campaign: {subdown.get('id')} ({subdown.get('type')})")
        
        request = {
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
        
        result = self.test_endpoint("POST", "/api/v1/calculate", request)
        if result and "quote" in result:
            # Check campaign audit
            if "campaign_audit" in result["quote"]:
                audit = result["quote"]["campaign_audit"]
                print(f"   Campaign audit has {len(audit)} entries")
                for entry in audit:
                    applied = entry.get("applied", False)
                    impact = entry.get("impact", 0)
                    print(f"     - {entry.get('campaign_id')}: applied={applied}, impact={impact}")
            else:
                self.warnings.append("No campaign_audit in response")
        return result
    
    def test_campaign_summaries(self, catalog):
        """Test campaign summaries endpoint"""
        print("\n=== Testing Campaign Summaries ===")
        
        if not catalog:
            self.warnings.append("No catalog available for summaries test")
            return None
        
        # Take first 3 campaigns for testing
        test_campaigns = []
        for item in catalog[:3]:
            test_campaigns.append({
                "id": item.get("id"),
                "type": item.get("type"),
                "parameters": item.get("parameters", {})
            })
        
        request = {
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
            "campaigns": test_campaigns
        }
        
        result = self.test_endpoint("POST", "/api/v1/campaigns/summaries", request)
        if result:
            print(f"   Received {len(result)} summaries")
            if len(result) > 0:
                # Check first summary structure
                first = result[0]
                required_fields = ["campaign_id", "campaign_type", "monthlyInstallment", 
                                 "customerRateEffective", "dealerCommissionAmt"]
                for field in required_fields:
                    if field not in first:
                        self.errors.append(f"Summary missing field: {field}")
                    
                # Validate numeric fields
                self.validate_number_field({"s": first}, "s", "monthlyInstallment")
                self.validate_number_field({"s": first}, "s", "customerRateEffective")
                self.validate_number_field({"s": first}, "s", "dealerCommissionAmt")
        return result
    
    def test_commission_auto(self):
        """Test commission auto endpoint"""
        print("\n=== Testing Commission Auto ===")
        
        products = ["HP", "F-Lease", "Op-Lease", "mySTAR"]
        for product in products:
            result = self.test_endpoint("GET", f"/api/v1/commission/auto?product={product}")
            if result:
                percent = result.get("percent", 0)
                print(f"   {product}: {percent * 100:.2f}%")
                self.validate_number_field({"r": result}, "r", "percent")
    
    def run_all_tests(self):
        """Run all tests and report results"""
        print("="*60)
        print("COMPREHENSIVE API TEST SUITE")
        print("="*60)
        
        # Check health first
        if not self.test_health():
            print("\nFATAL: Backend is not healthy. Exiting.")
            return False
        
        # Run tests
        self.test_parameters()
        catalog = self.test_campaign_catalog()
        self.test_calculation_basic()
        
        if catalog:
            self.test_calculation_with_campaign(catalog)
            self.test_campaign_summaries(catalog)
        
        self.test_commission_auto()
        
        # Report results
        print("\n" + "="*60)
        print("TEST RESULTS")
        print("="*60)
        
        if self.errors:
            print(f"\nERRORS ({len(self.errors)}):")
            for error in self.errors:
                print(f"  - {error}")
        
        if self.warnings:
            print(f"\nWARNINGS ({len(self.warnings)}):")
            for warning in self.warnings[:10]:  # Show first 10
                print(f"  - {warning}")
            if len(self.warnings) > 10:
                print(f"  ... and {len(self.warnings) - 10} more")
        
        if not self.errors:
            print("\nALL TESTS PASSED!")
            return True
        else:
            print(f"\nFAILED: {len(self.errors)} errors found")
            return False

def main():
    """Main entry point"""
    tester = APITester()
    success = tester.run_all_tests()
    sys.exit(0 if success else 1)

if __name__ == "__main__":
    main()