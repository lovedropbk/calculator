namespace FinancialCalculator.Engine.Models;

public enum PaymentMode
{
    InArrears = 0,
    InAdvance = 1
}

public enum FinancialProduct
{
    HirePurchase = 0,
    FinanceLease = 1,
    MySTAR = 2,
    OperatingLease = 3
}

public enum BalloonBase
{
    SalesPrice = 0,
    FinancedAmount = 1
}
