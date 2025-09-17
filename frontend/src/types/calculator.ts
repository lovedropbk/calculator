// Type definitions for Financial Calculator based on architecture spec

export type ProductType = 'HP' | 'mySTAR' | 'FinanceLease' | 'OperatingLease';
export type PaymentTiming = 'Arrears' | 'Advance';
export type PaymentFrequency = 'Monthly';
export type CampaignType = 'Subdown' | 'Subinterest' | 'FreeInsurance' | 'FreeMBSP' | 'CashDiscount';
export type IDCTiming = 'Upfront' | 'Periodic';
export type IDCCategory = 'DocumentationFee' | 'AcquisitionFee' | 'BrokerCommission' | 'Stamp' | 'InternalProcessing' | 'AdminMonthly' | 'Maintenance';

export interface Deal {
  // Product and market
  market: string;
  currency: string;
  product: ProductType;
  
  // Commercial terms
  priceExTax: number;
  downPaymentAmount?: number;
  downPaymentPercent?: number;
  downPaymentLocked?: 'amount' | 'percent';
  financedAmount?: number;
  termMonths: number;
  balloonPercent?: number; // For mySTAR only
  
  // Payment terms
  timing: PaymentTiming;
  paymentFrequency: PaymentFrequency;
  payoutDate: string;
  firstPaymentOffset: number;
  
  // Campaigns
  campaigns: CampaignType[];
  
  // Security (for leases)
  securityDeposit?: number;
}

export interface IDCItem {
  id: string;
  category: IDCCategory;
  type: 'Revenue' | 'Cost';
  timing: IDCTiming;
  financed: boolean;
  amount: number;
  taxable: boolean;
  payer: 'Customer' | 'Dealer' | 'Company';
  description?: string;
}

export interface QuoteResult {
  // Key metrics (always visible)
  monthlyInstallment: number;
  customerRateNominal: number;
  customerRateEffective: number;
  acquisitionRoRAC: number;
  
  // Profitability waterfall (collapsible)
  dealRateIRREffective?: number;
  costOfDebtMatchedFunded?: number;
  grossInterestMargin?: number;
  capitalAdvantage?: number;
  netInterestMargin?: number;
  standardCostOfCreditRisk?: number;
  opex?: number;
  idcSubsidiesAndFeesPeriodic?: number;
  netEBITMargin?: number;
  
  // Metadata
  parameterSetVersion?: string;
  calculationTimestamp?: string;
}

export interface ParameterSet {
  id: string;
  version: string;
  effectiveDate: string;
  lastSync?: string;
  
  // Parameters would be loaded from backend
  costOfFundsCurve?: any;
  matchedFundedSpread?: any;
  pdLgdTables?: any;
  opexRates?: any;
  economicCapitalParams?: any;
  centralHQAddOn?: any;
  roundingRules?: any;
}

export interface Scenario {
  id: string;
  name: string;
  deal: Deal;
  idcItems: IDCItem[];
  result?: QuoteResult;
  parameterSetVersion: string;
  createdAt: string;
  updatedAt?: string;
}