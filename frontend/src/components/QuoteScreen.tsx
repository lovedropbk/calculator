import React, { useState, useEffect } from 'react';
import { Deal, QuoteResult, IDCItem, ProductType, PaymentTiming, CampaignType } from '../types/calculator';
import { InputPanel } from './InputPanel';
import { ResultsPanel } from './ResultsPanel';
import { AdvancedDrawer } from './AdvancedDrawer';
import { IDCQuickAddModal } from './IDCQuickAddModal';
import { CalculateQuote } from '../../wailsjs/go/main/App';

interface QuoteScreenProps {
  parameterVersion?: string;
  lastSyncTime?: string;
}

export const QuoteScreen: React.FC<QuoteScreenProps> = ({
  parameterVersion = '2025.08',
  lastSyncTime
}) => {
  // Main deal state
  const [deal, setDeal] = useState<Deal>({
    market: 'TH',
    currency: 'THB',
    product: 'HP',
    priceExTax: 1000000, // Set default price for testing
    downPaymentPercent: 20,
    downPaymentAmount: 200000,
    downPaymentLocked: 'percent',
    termMonths: 36,
    timing: 'Arrears',
    paymentFrequency: 'Monthly',
    payoutDate: new Date().toISOString().split('T')[0],
    firstPaymentOffset: 1,
    campaigns: []
  });

  // IDC items
  const [idcItems, setIdcItems] = useState<IDCItem[]>([]);

  // Quote result from backend
  const [quoteResult, setQuoteResult] = useState<QuoteResult | null>(null);

  // UI state
  const [showAdvanced, setShowAdvanced] = useState(false);
  const [showIDCModal, setShowIDCModal] = useState(false);
  const [showDetails, setShowDetails] = useState(false);
  const [isCalculating, setIsCalculating] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // Rate mode state
  const [rateMode, setRateMode] = useState<'fixed_rate' | 'target_installment'>('fixed_rate');
  const [customerNominalRate, setCustomerNominalRate] = useState(0.0399); // 3.99% default
  const [targetInstallment, setTargetInstallment] = useState(0);

  // Calculate quote when inputs change
  const calculateQuote = async () => {
    setIsCalculating(true);
    setError(null);

    try {
      // Prepare parameters for backend call
      const downPaymentAmount = deal.downPaymentLocked === 'percent' 
        ? (deal.priceExTax * (deal.downPaymentPercent || 0) / 100)
        : (deal.downPaymentAmount || 0);
      
      const downPaymentPercent = deal.downPaymentLocked === 'amount'
        ? ((deal.downPaymentAmount || 0) / deal.priceExTax)
        : ((deal.downPaymentPercent || 0) / 100);

      // Convert campaigns to string array
      const campaignTypes = deal.campaigns || [];

      // Call backend calculation method
      const resultJSON = await CalculateQuote(
        deal.product,
        deal.priceExTax,
        downPaymentAmount,
        downPaymentPercent,
        deal.downPaymentLocked || 'percent',
        deal.termMonths,
        (deal.balloonPercent || 0) / 100,
        deal.timing,
        customerNominalRate,
        targetInstallment,
        rateMode,
        campaignTypes,
        JSON.stringify(idcItems)
      );

      // Parse and set result
      const result = JSON.parse(resultJSON);
      setQuoteResult(result);
    } catch (err) {
      console.error('Calculation error:', err);
      setError(err instanceof Error ? err.message : 'Calculation failed');
    } finally {
      setIsCalculating(false);
    }
  };

  // Trigger calculation on input changes (with debounce)
  useEffect(() => {
    const timer = setTimeout(() => {
      if (deal.priceExTax > 0 && deal.termMonths > 0) {
        calculateQuote();
      }
    }, 500);

    return () => clearTimeout(timer);
  }, [deal, idcItems, rateMode, customerNominalRate, targetInstallment]);

  // Handler for deal updates
  const updateDeal = (updates: Partial<Deal>) => {
    setDeal(prev => {
      const newDeal = { ...prev, ...updates };
      
      // Handle down payment locking logic
      if ('downPaymentPercent' in updates && prev.downPaymentLocked === 'percent') {
        newDeal.downPaymentAmount = newDeal.priceExTax * (newDeal.downPaymentPercent || 0) / 100;
      } else if ('downPaymentAmount' in updates && prev.downPaymentLocked === 'amount') {
        newDeal.downPaymentPercent = newDeal.priceExTax > 0 
          ? (newDeal.downPaymentAmount || 0) / newDeal.priceExTax * 100
          : 0;
      } else if ('priceExTax' in updates) {
        if (prev.downPaymentLocked === 'percent') {
          newDeal.downPaymentAmount = newDeal.priceExTax * (newDeal.downPaymentPercent || 0) / 100;
        } else {
          newDeal.downPaymentPercent = newDeal.priceExTax > 0
            ? (newDeal.downPaymentAmount || 0) / newDeal.priceExTax * 100
            : 0;
        }
      }
      
      return newDeal;
    });
  };

  // Handler for IDC updates
  const addIDCItem = (item: Omit<IDCItem, 'id'>) => {
    const newItem: IDCItem = {
      ...item,
      id: `idc_${Date.now()}`
    };
    setIdcItems(prev => [...prev, newItem]);
  };

  const removeIDCItem = (id: string) => {
    setIdcItems(prev => prev.filter(item => item.id !== id));
  };

  const updateIDCItem = (id: string, updates: Partial<IDCItem>) => {
    setIdcItems(prev => prev.map(item => 
      item.id === id ? { ...item, ...updates } : item
    ));
  };

  // Save scenario handler
  const handleSaveScenario = () => {
    const scenario = {
      deal,
      idcItems,
      quoteResult,
      rateMode,
      customerNominalRate,
      targetInstallment,
      timestamp: new Date().toISOString()
    };
    
    // Save to local storage for now
    const scenarios = JSON.parse(localStorage.getItem('scenarios') || '[]');
    scenarios.push(scenario);
    localStorage.setItem('scenarios', JSON.stringify(scenarios));
    
    console.log('Scenario saved:', scenario);
    alert('Scenario saved successfully!');
  };

  // Export handler
  const handleExport = () => {
    const exportData = {
      deal,
      idcItems,
      quoteResult,
      rateMode,
      customerNominalRate,
      targetInstallment,
      parameterVersion,
      timestamp: new Date().toISOString()
    };
    
    // Create download link
    const dataStr = JSON.stringify(exportData, null, 2);
    const dataUri = 'data:application/json;charset=utf-8,' + encodeURIComponent(dataStr);
    
    const exportFileDefaultName = `quote_${new Date().toISOString().slice(0, 10)}.json`;
    
    const linkElement = document.createElement('a');
    linkElement.setAttribute('href', dataUri);
    linkElement.setAttribute('download', exportFileDefaultName);
    linkElement.click();
  };

  return (
    <>
      {/* Header */}
      <header className="glass-card border-b border-white/10 px-6 py-4">
        <div className="flex items-center justify-between">
          <div className="flex items-center space-x-8">
            <h1 className="text-2xl font-bold text-white">Financial Calculator</h1>
            <div className="flex items-center space-x-4 text-sm">
              <div className="flex items-center space-x-2">
                <span className="text-white/60">Market:</span>
                <span className="text-white font-medium">{deal.market}</span>
              </div>
              <div className="flex items-center space-x-2">
                <span className="text-white/60">Currency:</span>
                <span className="text-white font-medium">{deal.currency}</span>
              </div>
              {parameterVersion && (
                <div className="flex items-center space-x-2">
                  <span className="text-white/60">Version:</span>
                  <span className="text-white font-medium">{parameterVersion}</span>
                </div>
              )}
            </div>
          </div>
          <div className="flex items-center space-x-3">
            {isCalculating && (
              <span className="text-yellow-400 text-sm">Calculating...</span>
            )}
            {error && (
              <span className="text-red-400 text-sm">{error}</span>
            )}
            <button
              onClick={handleSaveScenario}
              className="glass-button px-4 py-2"
              disabled={!quoteResult}
            >
              Save Scenario
            </button>
            <button
              onClick={handleExport}
              className="glass-button px-4 py-2"
              disabled={!quoteResult}
            >
              Export
            </button>
          </div>
        </div>
      </header>

      {/* Main Content */}
      <div className="flex-1 flex">
        <div className="container mx-auto px-6 py-6 max-w-7xl">
          <div className="grid grid-cols-2 gap-6 h-full">
            {/* Input Panel (Left) */}
            <div className="glass-card p-6 overflow-y-auto">
              <h2 className="text-lg font-semibold text-white mb-4">Inputs</h2>
              <InputPanel
                deal={deal}
                onDealChange={updateDeal}
                onShowAdvanced={() => setShowAdvanced(true)}
                onShowIDCModal={() => setShowIDCModal(true)}
              />
              
              {/* Rate Mode Selection */}
              <div className="mt-6 p-4 glass-card">
                <h3 className="text-sm font-medium text-white/80 mb-3">Rate Mode</h3>
                <div className="space-y-3">
                  <label className="flex items-center space-x-2">
                    <input
                      type="radio"
                      name="rateMode"
                      value="fixed_rate"
                      checked={rateMode === 'fixed_rate'}
                      onChange={(e) => setRateMode('fixed_rate')}
                      className="text-blue-500"
                    />
                    <span className="text-white">Fixed Rate</span>
                  </label>
                  {rateMode === 'fixed_rate' && (
                    <div className="ml-6">
                      <label className="block text-xs text-white/60 mb-1">
                        Customer Rate (% p.a.)
                      </label>
                      <input
                        type="number"
                        value={(customerNominalRate * 100).toFixed(2)}
                        onChange={(e) => setCustomerNominalRate(parseFloat(e.target.value) / 100)}
                        step="0.01"
                        className="w-32 px-2 py-1 bg-white/10 border border-white/20 rounded text-white"
                      />
                    </div>
                  )}
                  
                  <label className="flex items-center space-x-2">
                    <input
                      type="radio"
                      name="rateMode"
                      value="target_installment"
                      checked={rateMode === 'target_installment'}
                      onChange={(e) => setRateMode('target_installment')}
                      className="text-blue-500"
                    />
                    <span className="text-white">Target Installment</span>
                  </label>
                  {rateMode === 'target_installment' && (
                    <div className="ml-6">
                      <label className="block text-xs text-white/60 mb-1">
                        Target Installment (THB)
                      </label>
                      <input
                        type="number"
                        value={targetInstallment}
                        onChange={(e) => setTargetInstallment(parseFloat(e.target.value))}
                        step="100"
                        className="w-32 px-2 py-1 bg-white/10 border border-white/20 rounded text-white"
                      />
                    </div>
                  )}
                </div>
              </div>
            </div>

            {/* Results Panel (Right) */}
            <div className="glass-card p-6 overflow-y-auto">
              <h2 className="text-lg font-semibold text-white mb-4">Results</h2>
              <ResultsPanel
                result={quoteResult}
                showDetails={showDetails}
                onToggleDetails={() => setShowDetails(!showDetails)}
              />
            </div>
          </div>
        </div>
      </div>

      {/* Advanced Drawer */}
      {showAdvanced && (
        <AdvancedDrawer
          deal={deal}
          idcItems={idcItems}
          onDealChange={updateDeal}
          onAddIDC={addIDCItem}
          onUpdateIDC={updateIDCItem}
          onRemoveIDC={removeIDCItem}
          onClose={() => setShowAdvanced(false)}
        />
      )}

      {/* IDC Quick Add Modal */}
      {showIDCModal && (
        <IDCQuickAddModal
          onAdd={addIDCItem}
          onClose={() => setShowIDCModal(false)}
        />
      )}
    </>
  );
};