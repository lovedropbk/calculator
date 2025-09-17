// Type definitions for backend bindings

export interface Loan {
  amount: number;
  interestRate: number;
  termMonths: number;
  startDate?: string;
}

export interface AmortizationPayment {
  paymentNumber: number;
  paymentDate: string;
  paymentAmount: number;
  principal: number;
  interest: number;
  balance: number;
}

export interface AmortizationSchedule {
  loan: Loan;
  payments: AmortizationPayment[];
  totalInterest: number;
  totalPaid: number;
}

export interface PricingPlan {
  id: string;
  name: string;
  basePrice: number;
  discountPercentage?: number;
  markupPercentage?: number;
  validFrom: string;
  validTo: string;
}

export interface Product {
  id: string;
  name: string;
  basePrice: number;
  category: string;
  description?: string;
}

export interface Campaign {
  id: string;
  name: string;
  type: 'discount' | 'promotion' | 'seasonal';
  discountPercentage?: number;
  fixedDiscount?: number;
  startDate: string;
  endDate: string;
  active: boolean;
}

export interface Cashflow {
  id: string;
  type: 'income' | 'expense';
  amount: number;
  date: string;
  category: string;
  description?: string;
  recurring?: boolean;
  recurringInterval?: 'daily' | 'weekly' | 'monthly' | 'yearly';
}

export interface ProfitabilityAnalysis {
  revenue: number;
  cogs: number;
  operatingExpenses: number;
  grossProfit?: number;
  netProfit?: number;
  grossMargin?: number;
  netMargin?: number;
  roi?: number;
}

export interface CalculatorInputs {
  monthlyPayment?: number;
  loanAmount?: number;
  interestRate?: number;
  termMonths?: number;
}