import React, { useState } from 'react';
import { IDCItem, IDCCategory } from '../types/calculator';

interface IDCQuickAddModalProps {
  onAdd: (item: Omit<IDCItem, 'id'>) => void;
  onClose: () => void;
}

interface PresetIDC {
  label: string;
  category: IDCCategory;
  type: 'Revenue' | 'Cost';
  timing: 'Upfront' | 'Periodic';
  amount?: number;
  amountType?: 'fixed' | 'percent';
  percentOf?: 'price' | 'financed';
  financed: boolean;
  taxable: boolean;
  payer: 'Customer' | 'Dealer' | 'Company';
}

const presetIDCs: PresetIDC[] = [
  {
    label: 'Doc fee 2,000',
    category: 'DocumentationFee',
    type: 'Revenue',
    timing: 'Upfront',
    amount: 2000,
    amountType: 'fixed',
    financed: true,
    taxable: true,
    payer: 'Customer'
  },
  {
    label: 'Broker 1.5%',
    category: 'BrokerCommission',
    type: 'Cost',
    timing: 'Upfront',
    amount: 1.5,
    amountType: 'percent',
    percentOf: 'financed',
    financed: false,
    taxable: false,
    payer: 'Dealer'
  },
  {
    label: 'Stamp 0.5%',
    category: 'Stamp',
    type: 'Cost',
    timing: 'Upfront',
    amount: 0.5,
    amountType: 'percent',
    percentOf: 'financed',
    financed: false,
    taxable: false,
    payer: 'Customer'
  },
  {
    label: 'Admin 200/mth',
    category: 'AdminMonthly',
    type: 'Revenue',
    timing: 'Periodic',
    amount: 200,
    amountType: 'fixed',
    financed: false,
    taxable: true,
    payer: 'Customer'
  }
];

export const IDCQuickAddModal: React.FC<IDCQuickAddModalProps> = ({
  onAdd,
  onClose
}) => {
  const [customMode, setCustomMode] = useState(false);
  const [customIDC, setCustomIDC] = useState<Partial<Omit<IDCItem, 'id'>>>({
    type: 'Revenue',
    timing: 'Upfront',
    financed: false,
    taxable: true,
    payer: 'Customer'
  });

  const handlePresetClick = (preset: PresetIDC) => {
    // For percent-based presets, we'll need to calculate based on the deal amount
    // For now, we'll use a placeholder amount
    const amount = preset.amountType === 'percent' ? 10000 : preset.amount || 0;
    
    onAdd({
      category: preset.category,
      type: preset.type,
      timing: preset.timing,
      financed: preset.financed,
      amount: amount,
      taxable: preset.taxable,
      payer: preset.payer,
      description: preset.label
    });
    onClose();
  };

  const handleCustomAdd = () => {
    if (customIDC.category && customIDC.amount) {
      onAdd({
        category: customIDC.category,
        type: customIDC.type || 'Revenue',
        timing: customIDC.timing || 'Upfront',
        financed: customIDC.financed || false,
        amount: customIDC.amount,
        taxable: customIDC.taxable !== undefined ? customIDC.taxable : true,
        payer: customIDC.payer || 'Customer',
        description: customIDC.description
      });
      onClose();
    }
  };

  return (
    <>
      {/* Backdrop */}
      <div 
        className="fixed inset-0 bg-black/50 backdrop-blur-sm z-40"
        onClick={onClose}
      />
      
      {/* Modal */}
      <div className="fixed top-1/2 left-1/2 -translate-x-1/2 -translate-y-1/2 w-[500px] bg-gray-900/95 backdrop-blur-xl border border-white/20 rounded-lg p-6 z-50">
        <div className="flex justify-between items-center mb-6">
          <h2 className="text-xl font-semibold text-white">IDC Quick Add</h2>
          <button
            onClick={onClose}
            className="text-white/60 hover:text-white transition-colors"
          >
            <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
            </svg>
          </button>
        </div>

        {!customMode ? (
          <>
            {/* Presets */}
            <div className="space-y-4 mb-6">
              <h3 className="text-sm font-semibold text-white/80 uppercase tracking-wider">Presets</h3>
              <div className="grid grid-cols-2 gap-3">
                {presetIDCs.map((preset, index) => (
                  <button
                    key={index}
                    onClick={() => handlePresetClick(preset)}
                    className="glass-button px-4 py-3 text-left hover:bg-white/20"
                  >
                    <div className="text-white font-medium">{preset.label}</div>
                    <div className="text-xs text-white/60 mt-1">
                      {preset.type} · {preset.timing}
                    </div>
                  </button>
                ))}
              </div>
            </div>

            {/* Custom Button */}
            <div className="border-t border-white/10 pt-4">
              <button
                onClick={() => setCustomMode(true)}
                className="w-full glass-button px-4 py-3 flex items-center justify-center gap-2"
              >
                <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 4v16m8-8H4" />
                </svg>
                Custom IDC
              </button>
            </div>
          </>
        ) : (
          <>
            {/* Custom Form */}
            <div className="space-y-4">
              <h3 className="text-sm font-semibold text-white/80 uppercase tracking-wider">Custom IDC</h3>
              
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="text-sm text-white/60 block mb-1">Category</label>
                  <select
                    value={customIDC.category || ''}
                    onChange={(e) => setCustomIDC({ ...customIDC, category: e.target.value as IDCCategory })}
                    className="w-full px-3 py-2 rounded-lg bg-white/10 backdrop-blur-md border border-white/20 text-white focus:outline-none focus:ring-2 focus:ring-primary-400/50"
                  >
                    <option value="">Select...</option>
                    <option value="DocumentationFee">Documentation Fee</option>
                    <option value="AcquisitionFee">Acquisition Fee</option>
                    <option value="BrokerCommission">Broker Commission</option>
                    <option value="Stamp">Stamp</option>
                    <option value="InternalProcessing">Internal Processing</option>
                    <option value="AdminMonthly">Admin Monthly</option>
                    <option value="Maintenance">Maintenance</option>
                  </select>
                </div>

                <div>
                  <label className="text-sm text-white/60 block mb-1">Amount</label>
                  <input
                    type="number"
                    value={customIDC.amount || ''}
                    onChange={(e) => setCustomIDC({ ...customIDC, amount: parseFloat(e.target.value) })}
                    placeholder="Enter amount"
                    className="w-full px-3 py-2 rounded-lg bg-white/10 backdrop-blur-md border border-white/20 text-white placeholder-white/40 focus:outline-none focus:ring-2 focus:ring-primary-400/50"
                  />
                </div>

                <div>
                  <label className="text-sm text-white/60 block mb-1">Type</label>
                  <select
                    value={customIDC.type}
                    onChange={(e) => setCustomIDC({ ...customIDC, type: e.target.value as 'Revenue' | 'Cost' })}
                    className="w-full px-3 py-2 rounded-lg bg-white/10 backdrop-blur-md border border-white/20 text-white focus:outline-none focus:ring-2 focus:ring-primary-400/50"
                  >
                    <option value="Revenue">Revenue</option>
                    <option value="Cost">Cost</option>
                  </select>
                </div>

                <div>
                  <label className="text-sm text-white/60 block mb-1">Timing</label>
                  <select
                    value={customIDC.timing}
                    onChange={(e) => setCustomIDC({ ...customIDC, timing: e.target.value as 'Upfront' | 'Periodic' })}
                    className="w-full px-3 py-2 rounded-lg bg-white/10 backdrop-blur-md border border-white/20 text-white focus:outline-none focus:ring-2 focus:ring-primary-400/50"
                  >
                    <option value="Upfront">Upfront</option>
                    <option value="Periodic">Periodic</option>
                  </select>
                </div>

                <div>
                  <label className="text-sm text-white/60 block mb-1">Payer</label>
                  <select
                    value={customIDC.payer}
                    onChange={(e) => setCustomIDC({ ...customIDC, payer: e.target.value as 'Customer' | 'Dealer' | 'Company' })}
                    className="w-full px-3 py-2 rounded-lg bg-white/10 backdrop-blur-md border border-white/20 text-white focus:outline-none focus:ring-2 focus:ring-primary-400/50"
                  >
                    <option value="Customer">Customer</option>
                    <option value="Dealer">Dealer</option>
                    <option value="Company">Company</option>
                  </select>
                </div>

                <div>
                  <label className="text-sm text-white/60 block mb-1">Description</label>
                  <input
                    type="text"
                    value={customIDC.description || ''}
                    onChange={(e) => setCustomIDC({ ...customIDC, description: e.target.value })}
                    placeholder="Optional"
                    className="w-full px-3 py-2 rounded-lg bg-white/10 backdrop-blur-md border border-white/20 text-white placeholder-white/40 focus:outline-none focus:ring-2 focus:ring-primary-400/50"
                  />
                </div>
              </div>

              <div className="flex items-center gap-6">
                <label className="flex items-center gap-2 text-white/80">
                  <input
                    type="checkbox"
                    checked={customIDC.financed}
                    onChange={(e) => setCustomIDC({ ...customIDC, financed: e.target.checked })}
                    className="rounded border-white/20 bg-white/10"
                  />
                  <span className="text-sm">Financed</span>
                </label>

                <label className="flex items-center gap-2 text-white/80">
                  <input
                    type="checkbox"
                    checked={customIDC.taxable}
                    onChange={(e) => setCustomIDC({ ...customIDC, taxable: e.target.checked })}
                    className="rounded border-white/20 bg-white/10"
                  />
                  <span className="text-sm">Taxable</span>
                </label>
              </div>

              <div className="flex gap-3 pt-4">
                <button
                  onClick={handleCustomAdd}
                  disabled={!customIDC.category || !customIDC.amount}
                  className="flex-1 glass-button px-4 py-2 disabled:opacity-50"
                >
                  Add
                </button>
                <button
                  onClick={() => setCustomMode(false)}
                  className="flex-1 glass-button px-4 py-2"
                >
                  Back
                </button>
              </div>
            </div>
          </>
        )}
      </div>
    </>
  );
};