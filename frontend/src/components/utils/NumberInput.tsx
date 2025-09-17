import React, { useState, useEffect, useCallback } from 'react';

interface NumberInputProps {
  value?: number;
  onChange: (value: number | undefined) => void;
  placeholder?: string;
  label?: string;
  currency?: string;
  min?: number;
  max?: number;
  step?: number;
  disabled?: boolean;
  className?: string;
  formatOptions?: Intl.NumberFormatOptions;
}

export const NumberInput: React.FC<NumberInputProps> = ({
  value,
  onChange,
  placeholder = '',
  label,
  currency = 'THB',
  min,
  max,
  step = 1,
  disabled = false,
  className = '',
  formatOptions = { minimumFractionDigits: 0, maximumFractionDigits: 0 }
}) => {
  const [displayValue, setDisplayValue] = useState<string>('');
  const [isFocused, setIsFocused] = useState(false);

  // Format number for display
  const formatNumber = useCallback((num: number | undefined): string => {
    if (num === undefined || isNaN(num)) return '';
    
    if (currency === 'THB') {
      return new Intl.NumberFormat('th-TH', formatOptions).format(num);
    }
    return new Intl.NumberFormat('en-US', formatOptions).format(num);
  }, [currency, formatOptions]);

  // Parse display value to number
  const parseValue = useCallback((str: string): number | undefined => {
    const cleaned = str.replace(/[^\d.-]/g, '');
    const parsed = parseFloat(cleaned);
    return isNaN(parsed) ? undefined : parsed;
  }, []);

  // Update display when value changes
  useEffect(() => {
    if (!isFocused) {
      setDisplayValue(formatNumber(value));
    }
  }, [value, isFocused, formatNumber]);

  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const inputValue = e.target.value;
    setDisplayValue(inputValue);
    
    const numValue = parseValue(inputValue);
    if (numValue !== undefined) {
      if (min !== undefined && numValue < min) return;
      if (max !== undefined && numValue > max) return;
    }
    onChange(numValue);
  };

  const handleFocus = () => {
    setIsFocused(true);
    setDisplayValue(value?.toString() || '');
  };

  const handleBlur = () => {
    setIsFocused(false);
    setDisplayValue(formatNumber(value));
  };

  return (
    <div className={`space-y-1 ${className}`}>
      {label && (
        <label className="text-sm font-medium text-white/80">
          {label}
        </label>
      )}
      <div className="relative">
        {currency && !isFocused && displayValue && (
          <span className="absolute left-3 top-1/2 -translate-y-1/2 text-white/60 text-sm">
            {currency}
          </span>
        )}
        <input
          type="text"
          value={displayValue}
          onChange={handleChange}
          onFocus={handleFocus}
          onBlur={handleBlur}
          placeholder={placeholder}
          disabled={disabled}
          className={`
            w-full px-3 py-2 rounded-lg
            bg-white/10 backdrop-blur-md
            border border-white/20
            text-white placeholder-white/40
            focus:outline-none focus:ring-2 focus:ring-primary-400/50
            disabled:opacity-50 disabled:cursor-not-allowed
            transition-all duration-200
            ${currency && !isFocused && displayValue ? 'pl-12' : ''}
          `}
        />
      </div>
    </div>
  );
};

interface PercentInputProps extends Omit<NumberInputProps, 'currency' | 'formatOptions'> {
  decimalPlaces?: number;
}

export const PercentInput: React.FC<PercentInputProps> = ({
  decimalPlaces = 2,
  ...props
}) => {
  return (
    <NumberInput
      {...props}
      currency=""
      formatOptions={{
        minimumFractionDigits: decimalPlaces,
        maximumFractionDigits: decimalPlaces,
        style: 'percent'
      }}
    />
  );
};