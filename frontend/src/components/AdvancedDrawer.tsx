import React, { useState } from 'react';
import { Deal, IDCItem, IDCCategory } from '../types/calculator';
import { NumberInput } from './utils/NumberInput';
import { Select } from './utils/Select';

interface AdvancedDrawerProps {
  deal: Deal;
  idcItems: IDCItem[];
  onDealChange: (updates: Partial<Deal>) => void;
  onAddIDC: (item: Omit<IDCItem, 'id'>) => void;
  onUpdateIDC: (id: string, updates: Partial<IDCItem>) => void;
  onRemoveIDC: (id: string) => void;
  onClose: () => void;
}

export const AdvancedDrawer: React.FC<AdvancedDrawerProps> = ({
  deal,
  idcItems,
  onDealChange,
  onAddIDC,
  onUpdateIDC,
  onRemoveIDC,
  onClose
}) => {
  const [showAddIDC, setShowAddIDC] = useState(false);
  const [newIDC, setNewIDC] = useState<Partial<Omit<IDCItem, 'id'>>>({
    type: 'Revenue',
    timing: 'Upfront',
    financed: false,
    taxable: true,
    payer: 'Customer'
  });

  // Payment mode options
  const paymentModeOptions = [
    { value: 'Arrears' as const, label: 'In arrears' },
    { value: 'Advance' as const, label: 'In advance' }
  ];

  // Payment frequency options
  const frequencyOptions = [
    { value: 'Monthly' as const, label: 'Monthly' }
  ];

  const handleAddIDC = () => {
    if (newIDC.category && newIDC.amount) {
      onAddIDC({
        category: newIDC.category,
        type: newIDC.type || 'Revenue',
        timing: newIDC.timing || 'Upfront',
        financed: newIDC.financed || false,
        amount: newIDC.amount,
        taxable: newIDC.taxable !== undefined ? newIDC.taxable : true,
        payer: newIDC.payer || 'Customer',
        description: newIDC.description
      });
      // Reset form
      setNewIDC({
        type: 'Revenue',
        timing: 'Upfront',
        financed: false,
        taxable: true,
        payer: 'Customer'
      });
      setShowAddIDC(false);
    }
  };

  return (
    <>
      {/* Backdrop */}
      <div 
        className="fixed inset-0 bg-black/50 backdrop-blur-sm z-40"
        onClick={onClose}
      />
      
      {/* Drawer */}
      <div className="fixed right-0 top-0 h-full w-[600px] bg-gray-900/95 backdrop-blur-xl border-l border-white/10 z-50 overflow-y-auto">
        <div className="p-6">
          {/* Header */}
          <div className="flex justify-between items-center mb-6">
            <h2 className="text-xl font-semibold text-white">Advanced Settings</h2>
            <button
              onClick={onClose}
              className="text-white/60 hover:text-white transition-colors"
            >
              <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
              </svg>
            </button>
          </div>

          {/* Dates Section */}
          <div className="space-y-6 mb-8">
            <h3 className="text-sm font-semibold text-white/80 uppercase tracking-wider">Dates</h3>
            
            <div className="grid grid-cols-2 gap-4">
              <div>
                <label className="text-sm text-white/60 block mb-1">Payout Date</label>
                <input
                  type="date"
                  value={deal.payoutDate}
                  onChange={(e) => onDealChange({ payoutDate: e.target.value })}
                  className="w-full px-3 py-2 rounded-lg bg-white/10 backdrop-blur-md border border-white/20 text-white focus:outline-none focus:ring-2 focus:ring-primary-400/50"
                />
              </div>
              
              <NumberInput
                value={deal.firstPaymentOffset}
                onChange={(value) => onDealChange({ firstPaymentOffset: value || 1 })}
                label="First payment offset (months)"
                min={0}
                max={6}
                step={1}
              />
            </div>
          </div>

          {/* Payment Settings */}
          <div className="space-y-6 mb-8">
            <h3 className="text-sm font-semibold text-white/80 uppercase tracking-wider">Payment Settings</h3>
            
            <div className="grid grid-cols-2 gap-4">
              <Select
                value={deal.timing}
                onChange={(value) => onDealChange({ timing: value })}
                options={paymentModeOptions}
                label="Payment mode"
              />
              
              <Select
                value={deal.paymentFrequency}
                onChange={(value) => onDealChange({ paymentFrequency: value })}
                options={frequencyOptions}
                label="Frequency"
              />
            </div>
          </div>

          {/* Security Deposit (for leases) */}
          {(deal.product === 'FinanceLease' || deal.product === 'OperatingLease') && (
            <div className="space-y-6 mb-8">
              <h3 className="text-sm font-semibold text-white/80 uppercase tracking-wider">Security</h3>
              
              <NumberInput
                value={deal.securityDeposit}
                onChange={(value) => onDealChange({ securityDeposit: value })}
                label="Security deposit"
                currency="THB"
                placeholder="Enter amount"
                min={0}
              />
            </div>
          )}

          {/* IDC Table */}
          <div className="space-y-6">
            <div className="flex justify-between items-center">
              <h3 className="text-sm font-semibold text-white/80 uppercase tracking-wider">IDC Items</h3>
              <button
                onClick={() => setShowAddIDC(true)}
                className="glass-button px-3 py-1.5 text-sm"
              >
                + Add
              </button>
            </div>

            {idcItems.length > 0 ? (
              <div className="overflow-x-auto">
                <table className="w-full text-sm">
                  <thead>
                    <tr className="border-b border-white/10">
                      <th className="text-left py-2 text-white/60 font-normal">Item</th>
                      <th className="text-left py-2 text-white/60 font-normal">Type</th>
                      <th className="text-left py-2 text-white/60 font-normal">Timing</th>
                      <th className="text-left py-2 text-white/60 font-normal">Financed</th>
                      <th className="text-right py-2 text-white/60 font-normal">Amount</th>
                      <th className="text-center py-2 text-white/60 font-normal">Tax</th>
                      <th className="text-left py-2 text-white/60 font-normal">Payer</th>
                      <th className="text-center py-2 text-white/60 font-normal"></th>
                    </tr>
                  </thead>
                  <tbody>
                    {idcItems.map((item) => (
                      <tr key={item.id} className="border-b border-white/5">
                        <td className="py-2 text-white/80">{item.category}</td>
                        <td className="py-2 text-white/80">{item.type}</td>
                        <td className="py-2 text-white/80">{item.timing}</td>
                        <td className="py-2 text-white/80">{item.financed ? 'Yes' : 'No'}</td>
                        <td className="py-2 text-white/80 text-right">
                          {new Intl.NumberFormat('th-TH').format(item.amount)}
                        </td>
                        <td className="py-2 text-white/80 text-center">
                          {item.taxable ? 'VAT' : '-'}
                        </td>
                        <td className="py-2 text-white/80">{item.payer}</td>
                        <td className="py-2 text-center">
                          <button
                            onClick={() => onRemoveIDC(item.id)}
                            className="text-red-400 hover:text-red-300"
                          >
                            <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
                            </svg>
                          </button>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            ) : (
              <div className="text-center py-8 text-white/40">
                No IDC items added yet
              </div>
            )}
          </div>
        </div>
      </div>

      {/* Add IDC Modal */}
      {showAddIDC && (
        <div className="fixed inset-0 bg-black/50 backdrop-blur-sm z-60 flex items-center justify-center">
          <div className="bg-gray-900/95 backdrop-blur-xl border border-white/20 rounded-lg p-6 w-[400px]">
            <h3 className="text-lg font-semibold text-white mb-4">Add IDC Item</h3>
            
            <div className="space-y-4">
              <Select
                value={newIDC.category}
                onChange={(value) => setNewIDC({ ...newIDC, category: value as IDCCategory })}
                options={[
                  { value: 'DocumentationFee' as IDCCategory, label: 'Documentation Fee' },
                  { value: 'AcquisitionFee' as IDCCategory, label: 'Acquisition Fee' },
                  { value: 'BrokerCommission' as IDCCategory, label: 'Broker Commission' },
                  { value: 'Stamp' as IDCCategory, label: 'Stamp' },
                  { value: 'InternalProcessing' as IDCCategory, label: 'Internal Processing' },
                  { value: 'AdminMonthly' as IDCCategory, label: 'Admin Monthly' },
                  { value: 'Maintenance' as IDCCategory, label: 'Maintenance' }
                ]}
                label="Category"
                placeholder="Select category"
              />

              <Select
                value={newIDC.type}
                onChange={(value) => setNewIDC({ ...newIDC, type: value as 'Revenue' | 'Cost' })}
                options={[
                  { value: 'Revenue', label: 'Revenue' },
                  { value: 'Cost', label: 'Cost' }
                ]}
                label="Type"
              />

              <Select
                value={newIDC.timing}
                onChange={(value) => setNewIDC({ ...newIDC, timing: value as 'Upfront' | 'Periodic' })}
                options={[
                  { value: 'Upfront', label: 'Upfront' },
                  { value: 'Periodic', label: 'Periodic' }
                ]}
                label="Timing"
              />

              <NumberInput
                value={newIDC.amount}
                onChange={(value) => setNewIDC({ ...newIDC, amount: value })}
                label="Amount"
                currency="THB"
                placeholder="Enter amount"
                min={0}
              />

              <div className="flex items-center gap-4">
                <label className="flex items-center gap-2 text-white/80">
                  <input
                    type="checkbox"
                    checked={newIDC.financed}
                    onChange={(e) => setNewIDC({ ...newIDC, financed: e.target.checked })}
                    className="rounded border-white/20 bg-white/10"
                  />
                  <span className="text-sm">Financed</span>
                </label>

                <label className="flex items-center gap-2 text-white/80">
                  <input
                    type="checkbox"
                    checked={newIDC.taxable}
                    onChange={(e) => setNewIDC({ ...newIDC, taxable: e.target.checked })}
                    className="rounded border-white/20 bg-white/10"
                  />
                  <span className="text-sm">Taxable</span>
                </label>
              </div>

              <Select
                value={newIDC.payer}
                onChange={(value) => setNewIDC({ ...newIDC, payer: value as 'Customer' | 'Dealer' | 'Company' })}
                options={[
                  { value: 'Customer', label: 'Customer' },
                  { value: 'Dealer', label: 'Dealer' },
                  { value: 'Company', label: 'Company' }
                ]}
                label="Payer"
              />
            </div>

            <div className="flex gap-3 mt-6">
              <button
                onClick={handleAddIDC}
                className="flex-1 glass-button px-4 py-2"
              >
                Add
              </button>
              <button
                onClick={() => setShowAddIDC(false)}
                className="flex-1 glass-button px-4 py-2"
              >
                Cancel
              </button>
            </div>
          </div>
        </div>
      )}
    </>
  );
};