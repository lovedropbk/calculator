package main

import (
	"fmt"
	"math"
	"strings"

	"github.com/financial-calculator/engines/types"
)

// MapProductDisplayToEnum maps UI-facing product display names to engine enums.
// Accepts modern labels and legacy short codes for robustness.
func MapProductDisplayToEnum(display string) (types.Product, error) {
	s := strings.TrimSpace(strings.ToLower(display))
	switch s {
	case "hp", "hire purchase":
		return types.ProductHirePurchase, nil
	case "mystar", "my star", "my&#x2a;star", "my&#x2a; star":
		return types.ProductMySTAR, nil
	case "financing lease", "finance lease", "f-lease", "f lease", "flease":
		return types.ProductFinanceLease, nil
	case "operating lease", "op-lease", "op lease", "oplease":
		return types.ProductOperatingLease, nil
	default:
		return "", fmt.Errorf("unknown product display: %q", display)
	}
}

// FormatTHB formats a THB amount with thousands separators.
// Rule:
// - If the value is an integer (after 2dp rounding), show no decimal places.
// - Otherwise, show exactly 2 decimal places.
// Examples: 22198.61 -> "22,198.61"; 12000 -> "12,000"
func FormatTHB(amount float64) string {
	// Round to 2 decimals for stability
	rounded := math.Round(amount*100) / 100

	// Detect integer after rounding to 2dp
	if math.Abs(rounded-math.Round(rounded)) < 1e-9 {
		intVal := int64(math.Round(rounded))
		return addThousandsSep(fmt.Sprintf("%d", intVal))
	}

	s := fmt.Sprintf("%.2f", rounded)
	// Split int/dec
	dot := strings.LastIndexByte(s, '.')
	if dot < 0 {
		return addThousandsSep(s)
	}
	intPart := s[:dot]
	decPart := s[dot:] // includes dot
	return addThousandsSep(intPart) + decPart
}

// FormatRatePct formats a fractional rate (e.g., 0.0558) as a human string "5.58 percent".
func FormatRatePct(p float64) string {
	return fmt.Sprintf("%.2f percent", p*100.0)
}

// FormatDealerCommission renders "THB X (Y%)" with compact zero formatting for cash discount rows.
// Special case: zero amount and zero pct renders "THB 0 (0%)".
func FormatDealerCommission(amountTHB float64, pct float64) string {
	if math.Abs(amountTHB) < 0.5 && math.Abs(pct) < 1e-12 {
		return "THB 0 (0%)"
	}
	return fmt.Sprintf("THB %s (%.2f%%)", FormatTHB(amountTHB), pct*100.0)
}

// addThousandsSep inserts commas as thousands separators in a purely integer string.
func addThousandsSep(intStr string) string {
	n := len(intStr)
	if n <= 3 {
		return intStr
	}
	var out []byte
	neg := false
	start := 0
	if strings.HasPrefix(intStr, "-") {
		neg = true
		start = 1
	}
	digits := intStr[start:]
	// process from end
	count := 0
	for i := len(digits) - 1; i >= 0; i-- {
		out = append([]byte{digits[i]}, out...)
		count++
		if count%3 == 0 && i != 0 {
			out = append([]byte{','}, out...)
		}
	}
	if neg {
		return "-" + string(out)
	}
	return string(out)
}
