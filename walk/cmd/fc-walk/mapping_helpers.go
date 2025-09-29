package main

import (
	"fmt"
	"math"
	"strconv"
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

// RoundTo rounds a float64 to the given number of decimal places.
func RoundTo(val float64, decimals int) float64 {
	if decimals <= 0 {
		return math.Round(val)
	}
	pow := math.Pow(10, float64(decimals))
	return math.Round(val*pow) / pow
}

// FormatWithThousandSep formats a number with thousands separators and a fixed number of decimals.
// Examples:
//
//	FormatWithThousandSep(1000000, 0)   -> "1,000,000"
//	FormatWithThousandSep(22198.61, 2) -> "22,198.61"
func FormatWithThousandSep(val float64, decimals int) string {
	v := RoundTo(val, decimals)
	neg := v < 0
	if neg {
		v = -v
	}
	var s string
	if decimals <= 0 {
		s = addThousandsSep(fmt.Sprintf("%.0f", v))
	} else {
		// Build a format string like "%.2f"
		fs := "%." + strconv.Itoa(decimals) + "f"
		raw := fmt.Sprintf(fs, v)
		dot := strings.LastIndexByte(raw, '.')
		if dot < 0 {
			s = addThousandsSep(raw)
		} else {
			intPart := raw[:dot]
			decPart := raw[dot:]
			s = addThousandsSep(intPart) + decPart
		}
	}
	if neg {
		return "-" + s
	}
	return s
}

// ParseThousand parses a string that may contain thousands separators (commas) into a float64.
func ParseThousand(s string) (float64, error) {
	cleaned := strings.TrimSpace(strings.ReplaceAll(s, ",", ""))
	if cleaned == "" {
		return 0, nil
	}
	return strconv.ParseFloat(cleaned, 64)
}

// sanitizeMonthlyForRow converts a UI label text into a numeric string for the My Campaigns row.
// Rules:
// - "THB 12,345.67" -> "12,345.67"
// - "—" or "-" or empty -> ""
// - "12,345.67" (already numeric) -> "12,345.67"
// Keeps commas and decimal point; strips "THB" and spaces.
func sanitizeMonthlyForRow(input string) string {
	s := strings.TrimSpace(input)
	if s == "" {
		return ""
	}
	if s == "—" || s == "-" {
		return ""
	}
	// Remove any occurrence of "THB" then strip all spaces
	s = strings.ReplaceAll(s, "THB", "")
	s = strings.ReplaceAll(s, " ", "")
	s = strings.TrimSpace(s)
	if s == "" || s == "—" || s == "-" {
		return ""
	}
	return s
}
