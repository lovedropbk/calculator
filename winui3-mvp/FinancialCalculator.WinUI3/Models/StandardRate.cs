namespace FinancialCalculator.WinUI3.Models;

public class StandardRate
{
    public string Product { get; set; } = string.Empty;
    public int Term { get; set; }
    public double DPMin { get; set; }
    public double DPMax { get; set; }
    public string PaymentMode { get; set; } = string.Empty; // Advance, Arrears, Any
    public double Rate { get; set; }

    public bool Matches(string product, int term, double downPaymentPct, string paymentMode)
    {
        if (!string.Equals(Product, product, System.StringComparison.OrdinalIgnoreCase)) return false;
        if (Term != term) return false;
        
        // Handle precision issues with down payment ranges
        // Using a small epsilon for comparisons if needed, but direct double comparison might be okay for these ranges if data is clean.
        // Let's use >= and <= for inclusive ranges as per typical business logic, but the CSV seems to have distinct ranges e.g. 0.00-0.1499, 0.15-0.1999.
        if (downPaymentPct < DPMin || downPaymentPct > DPMax) return false;

        if (string.Equals(PaymentMode, "Any", System.StringComparison.OrdinalIgnoreCase)) return true;
        
        // Map "in advance" from UI to "Advance" in CSV if necessary, or ensure consistency.
        // UI uses "advance" (lowercase) and "arrears" (lowercase) in ComboBox tags.
        // CSV uses "Advance" and "Arrears".
        string normalizedMode = paymentMode.ToLowerInvariant() == "advance" || paymentMode.ToLowerInvariant() == "in advance" ? "Advance" :
                                paymentMode.ToLowerInvariant() == "arrears" ? "Arrears" : paymentMode;

        return string.Equals(PaymentMode, normalizedMode, System.StringComparison.OrdinalIgnoreCase);
    }
}