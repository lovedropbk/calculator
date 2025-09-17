import React from 'react';
import { QuoteResult } from '../types/calculator';

interface ResultsPanelProps {
  result: QuoteResult | null;
  showDetails: boolean;
  onToggleDetails: () => void;
}

export const ResultsPanel: React.FC<ResultsPanelProps> = ({
  result,
  showDetails,
  onToggleDetails
}) => {
  // Format currency
  const formatCurrency = (value: number): string => {
    return new Intl.NumberFormat('th-TH', {
      minimumFractionDigits: 2,
      maximumFractionDigits: 2
    }).format(value);
  };

  // Format percentage
  const formatPercent = (value: number): string => {
    return `${value.toFixed(2)}%`;
  };

  if (!result) {
    return (
      <div className="space-y-4">
        <div className="text-center py-12">
          <div className="inline-flex items-center justify-center w-16 h-16 rounded-full bg-white/10 mb-4">
            <svg className="w-8 h-8 text-white/40" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 7h6m0 10v-3m-3 3h.01M9 17h.01M9 14h.01M12 14h.01M15 11h.01M12 11h.01M9 11h.01M7 21h10a2 2 0 002-2V5a2 2 0 00-2-2H7a2 2 0 00-2 2v14a2 2 0 002 2z" />
            </svg>
          </div>
          <p className="text-white/60">Enter values to calculate</p>
          <p className="text-white/40 text-sm mt-2">Results will appear here</p>
        </div>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Key Metrics (Always Visible) */}
      <div className="space-y-4">
        <div className="bg-gradient-to-r from-primary-500/20 to-primary-600/20 backdrop-blur-md rounded-lg p-4 border border-primary-400/30">
          <div className="flex justify-between items-center">
            <span className="text-white/80 text-sm">Monthly Installment</span>
            <span className="text-2xl font-bold text-white">
              THB {formatCurrency(result.monthlyInstallment)}
            </span>
          </div>
        </div>

        <div className="grid grid-cols-2 gap-4">
          <div className="bg-white/5 backdrop-blur-md rounded-lg p-4 border border-white/10">
            <div className="text-white/60 text-xs mb-1">Customer Rate Nominal</div>
            <div className="text-xl font-semibold text-white">
              {formatPercent(result.customerRateNominal)}
            </div>
          </div>

          <div className="bg-white/5 backdrop-blur-md rounded-lg p-4 border border-white/10">
            <div className="text-white/60 text-xs mb-1">Customer Rate Effective</div>
            <div className="text-xl font-semibold text-white">
              {formatPercent(result.customerRateEffective)}
            </div>
          </div>
        </div>

        <div className="bg-white/5 backdrop-blur-md rounded-lg p-4 border border-white/10">
          <div className="flex justify-between items-center">
            <span className="text-white/80 text-sm">Acquisition RoRAC</span>
            <span className="text-xl font-semibold text-white">
              {formatPercent(result.acquisitionRoRAC)}
            </span>
          </div>
        </div>
      </div>

      {/* Details Toggle Button */}
      <button
        onClick={onToggleDetails}
        className="w-full flex items-center justify-center gap-2 py-2 px-4 rounded-lg bg-white/5 hover:bg-white/10 border border-white/20 transition-all duration-200"
      >
        <span className="text-white/80 text-sm">Details</span>
        <span className={`text-white/60 transition-transform duration-200 ${showDetails ? 'rotate-180' : ''}`}>
          ▼
        </span>
      </button>

      {/* Profitability Waterfall (Collapsible) */}
      {showDetails && (
        <div className="animate-slide-up space-y-3">
          <h3 className="text-sm font-semibold text-white/80 mb-3">Profitability Waterfall</h3>
          
          <div className="space-y-2">
            {result.dealRateIRREffective !== undefined && (
              <WaterfallItem
                label="Deal Rate IRR Effective"
                value={formatPercent(result.dealRateIRREffective)}
              />
            )}
            
            {result.costOfDebtMatchedFunded !== undefined && (
              <WaterfallItem
                label="Cost of Debt Matched Funded"
                value={formatPercent(result.costOfDebtMatchedFunded)}
                isNegative
              />
            )}
            
            {result.grossInterestMargin !== undefined && (
              <WaterfallItem
                label="Gross Interest Margin"
                value={formatPercent(result.grossInterestMargin)}
                isSubtotal
              />
            )}
            
            {result.capitalAdvantage !== undefined && (
              <WaterfallItem
                label="Capital Advantage"
                value={formatPercent(result.capitalAdvantage)}
              />
            )}
            
            {result.netInterestMargin !== undefined && (
              <WaterfallItem
                label="Net Interest Margin"
                value={formatPercent(result.netInterestMargin)}
                isSubtotal
              />
            )}
            
            {result.standardCostOfCreditRisk !== undefined && (
              <WaterfallItem
                label="Standard Cost of Credit Risk"
                value={formatPercent(result.standardCostOfCreditRisk)}
                isNegative
              />
            )}
            
            {result.opex !== undefined && (
              <WaterfallItem
                label="OPEX"
                value={formatPercent(result.opex)}
                isNegative
              />
            )}
            
            {result.idcSubsidiesAndFeesPeriodic !== undefined && (
              <WaterfallItem
                label="IDC Subsidies and Fees (periodic)"
                value={formatPercent(result.idcSubsidiesAndFeesPeriodic)}
              />
            )}
            
            {result.netEBITMargin !== undefined && (
              <WaterfallItem
                label="Net EBIT Margin"
                value={formatPercent(result.netEBITMargin)}
                isSubtotal
              />
            )}
            
            <div className="pt-2 mt-2 border-t border-white/20">
              <WaterfallItem
                label="Acquisition RoRAC"
                value={formatPercent(result.acquisitionRoRAC)}
                isFinal
              />
            </div>
          </div>
        </div>
      )}

      {/* Metadata */}
      {(result.parameterSetVersion || result.calculationTimestamp) && (
        <div className="pt-4 border-t border-white/10">
          <div className="text-xs text-white/40 space-y-1">
            {result.parameterSetVersion && (
              <div>Parameter Version: {result.parameterSetVersion}</div>
            )}
            {result.calculationTimestamp && (
              <div>Calculated: {new Date(result.calculationTimestamp).toLocaleString()}</div>
            )}
          </div>
        </div>
      )}
    </div>
  );
};

// Helper component for waterfall items
interface WaterfallItemProps {
  label: string;
  value: string;
  isNegative?: boolean;
  isSubtotal?: boolean;
  isFinal?: boolean;
}

const WaterfallItem: React.FC<WaterfallItemProps> = ({
  label,
  value,
  isNegative = false,
  isSubtotal = false,
  isFinal = false
}) => {
  const bgClass = isFinal 
    ? 'bg-primary-500/20 border-primary-400/30'
    : isSubtotal 
    ? 'bg-white/10 border-white/20' 
    : 'bg-white/5 border-white/10';
    
  const textClass = isFinal || isSubtotal ? 'font-semibold' : '';
  const valueClass = isNegative ? 'text-red-400' : 'text-white';

  return (
    <div className={`flex justify-between items-center px-3 py-2 rounded-lg ${bgClass} border backdrop-blur-md`}>
      <span className={`text-sm text-white/80 ${textClass}`}>{label}</span>
      <span className={`text-sm ${valueClass} ${textClass}`}>{value}</span>
    </div>
  );
};