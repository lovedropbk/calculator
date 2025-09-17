import React, { useState, useEffect } from 'react';
import { Deal, ProductType, PaymentTiming, CampaignType } from '../types/calculator';
import { ToggleGroup } from './utils/ToggleGroup';
import { NumberInput, PercentInput } from './utils/NumberInput';
import { Select } from './utils/Select';
import { ChipSelector } from './utils/ChipSelector';

interface InputPanelProps {
  deal: Deal;
  onDealChange: (updates: Partial<Deal>) => void;
  onShowAdvanced: () => void;
  onShowIDCModal: () => void;
}

export const InputPanel: React.FC<InputPanelProps> = ({
  deal,
  onDealChange,
  onShowAdvanced,
  onShowIDCModal
}) => {
  const [downPaymentLocked, setDownPaymentLocked] = useState<'amount' | 'percent'>('percent');

  // Product options
  const productOptions = [
    { value: 'HP' as ProductType, label: 'HP' },
    { value: 'mySTAR' as ProductType, label: 'mySTAR' },
    { value: 'FinanceLease' as ProductType, label: 'F-Lease' },
    { value: 'OperatingLease' as ProductType, label: 'Op-Lease' }
  ];

  // Term options (months)
  const termOptions = [
    { value: 12, label: '12 months' },
    { value: 24, label: '24 months' },
    { value: 36, label: '36 months' },
    { value: 48, label: '48 months' },
    { value: 60, label: '60 months' }
  ];

  // Timing options
  const timingOptions = [
    { value: 'Arrears' as PaymentTiming, label: 'Arrears' },
    { value: 'Advance' as PaymentTiming, label: 'Advance' }
  ];

  // Campaign options
  const campaignOptions = [
    { value: 'Subdown' as CampaignType, label: 'Subdown' },
    { value: 'Subinterest' as CampaignType, label: 'Subint' },
    { value: 'FreeInsurance' as CampaignType, label: 'Free Ins' },
    { value: 'FreeMBSP' as CampaignType, label: 'MBSP' },
    { value: 'CashDiscount' as CampaignType, label: 'Cash Discount' }
  ];

  // Handle down payment changes with two-way binding
  useEffect(() => {
    if (deal.priceExTax && deal.priceExTax > 0) {
      if (downPaymentLocked === 'percent' && deal.downPaymentPercent !== undefined) {
        const amount = Math.round(deal.priceExTax * (deal.downPaymentPercent / 100));
        if (amount !== deal.downPaymentAmount) {
          onDealChange({ downPaymentAmount: amount });
        }
      } else if (downPaymentLocked === 'amount' && deal.downPaymentAmount !== undefined) {
        const percent = (deal.downPaymentAmount / deal.priceExTax) * 100;
        if (Math.abs(percent - (deal.downPaymentPercent || 0)) > 0.01) {
          onDealChange({ downPaymentPercent: percent });
        }
      }
    }
  }, [deal.priceExTax, deal.downPaymentAmount, deal.downPaymentPercent, downPaymentLocked, onDealChange]);

  const handleDownPaymentAmountChange = (value: number | undefined) => {
    setDownPaymentLocked('amount');
    onDealChange({ 
      downPaymentAmount: value,
      downPaymentLocked: 'amount'
    });
  };

  const handleDownPaymentPercentChange = (value: number | undefined) => {
    setDownPaymentLocked('percent');
    onDealChange({ 
      downPaymentPercent: value,
      downPaymentLocked: 'percent'
    });
  };

  return (
    <div className="space-y-6">
      {/* Product Selector */}
      <ToggleGroup
        value={deal.product}
        onChange={(product) => onDealChange({ product })}
        options={productOptions}
        label="Product"
      />

      {/* Price Input */}
      <NumberInput
        value={deal.priceExTax}
        onChange={(value) => onDealChange({ priceExTax: value || 0 })}
        label="Price ex tax"
        currency="THB"
        placeholder="Enter price"
        min={0}
      />

      {/* Down Payment Dual Input */}
      <div className="space-y-2">
        <div className="flex items-center justify-between">
          <label className="text-sm font-medium text-white/80">Down payment</label>
          <button
            onClick={() => setDownPaymentLocked(downPaymentLocked === 'amount' ? 'percent' : 'amount')}
            className="text-xs text-primary-400 hover:text-primary-300"
          >
            {downPaymentLocked === 'amount' ? '🔒 THB' : '🔒 %'}
          </button>
        </div>
        <div className="grid grid-cols-2 gap-3">
          <NumberInput
            value={deal.downPaymentAmount}
            onChange={handleDownPaymentAmountChange}
            placeholder="THB amount"
            currency="THB"
            min={0}
            max={deal.priceExTax ? deal.priceExTax * 0.8 : undefined}
          />
          <div className="relative">
            <input
              type="number"
              value={deal.downPaymentPercent || ''}
              onChange={(e) => handleDownPaymentPercentChange(parseFloat(e.target.value) || undefined)}
              placeholder="Percent"
              min={0}
              max={80}
              step={0.1}
              className="w-full px-3 py-2 pr-8 rounded-lg bg-white/10 backdrop-blur-md border border-white/20 text-white placeholder-white/40 focus:outline-none focus:ring-2 focus:ring-primary-400/50"
            />
            <span className="absolute right-3 top-1/2 -translate-y-1/2 text-white/60 text-sm">%</span>
          </div>
        </div>
        <p className="text-xs text-white/50">0-80% range, rounded to whole THB</p>
      </div>

      {/* Term Selector */}
      <Select
        value={deal.termMonths}
        onChange={(value) => onDealChange({ termMonths: value })}
        options={termOptions}
        label="Term months"
      />

      {/* Balloon (only for mySTAR) */}
      {deal.product === 'mySTAR' && (
        <div className="relative">
          <label className="text-sm font-medium text-white/80 block mb-1">
            Balloon percent
          </label>
          <input
            type="number"
            value={deal.balloonPercent || ''}
            onChange={(e) => onDealChange({ balloonPercent: parseFloat(e.target.value) || undefined })}
            placeholder="Enter balloon %"
            min={0}
            max={50}
            step={0.1}
            className="w-full px-3 py-2 pr-8 rounded-lg bg-white/10 backdrop-blur-md border border-white/20 text-white placeholder-white/40 focus:outline-none focus:ring-2 focus:ring-primary-400/50"
          />
          <span className="absolute right-3 bottom-2 text-white/60 text-sm">%</span>
        </div>
      )}

      {/* Timing Selector */}
      <Select
        value={deal.timing}
        onChange={(value) => onDealChange({ timing: value })}
        options={timingOptions}
        label="Timing"
      />

      {/* Campaign Chips */}
      <ChipSelector
        selected={deal.campaigns}
        onChange={(campaigns) => onDealChange({ campaigns })}
        options={campaignOptions}
        label="Campaigns"
        multiple={true}
      />

      {/* Action Buttons */}
      <div className="flex gap-3 pt-4">
        <button
          onClick={onShowIDCModal}
          className="flex-1 glass-button px-4 py-2.5 flex items-center justify-center gap-2"
        >
          <span className="text-lg">+</span>
          IDC Quick Add
        </button>
        <button
          onClick={onShowAdvanced}
          className="flex-1 glass-button px-4 py-2.5 flex items-center justify-center gap-2"
        >
          Advanced
          <span className="text-sm">▸</span>
        </button>
      </div>
    </div>
  );
};