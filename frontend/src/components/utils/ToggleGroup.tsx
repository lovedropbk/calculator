import React from 'react';

interface ToggleOption<T = string> {
  value: T;
  label: string;
  disabled?: boolean;
}

interface ToggleGroupProps<T = string> {
  value: T;
  onChange: (value: T) => void;
  options: ToggleOption<T>[];
  label?: string;
  className?: string;
}

export function ToggleGroup<T extends string = string>({
  value,
  onChange,
  options,
  label,
  className = ''
}: ToggleGroupProps<T>) {
  return (
    <div className={`space-y-1 ${className}`}>
      {label && (
        <label className="text-sm font-medium text-white/80">
          {label}
        </label>
      )}
      <div className="inline-flex rounded-lg overflow-hidden border border-white/20">
        {options.map((option, index) => (
          <button
            key={String(option.value)}
            onClick={() => !option.disabled && onChange(option.value)}
            disabled={option.disabled}
            className={`
              px-4 py-2 text-sm font-medium transition-all duration-200
              ${value === option.value
                ? 'bg-primary-500/30 text-white border-primary-400'
                : 'bg-white/5 text-white/70 hover:bg-white/10'
              }
              ${option.disabled ? 'opacity-50 cursor-not-allowed' : 'cursor-pointer'}
              ${index > 0 ? 'border-l border-white/20' : ''}
              focus:outline-none focus:ring-2 focus:ring-primary-400/50
            `}
          >
            {option.label}
          </button>
        ))}
      </div>
    </div>
  );
}