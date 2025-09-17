import React from 'react';

interface ChipSelectorProps<T = string> {
  selected: T[];
  onChange: (selected: T[]) => void;
  options: Array<{
    value: T;
    label: string;
    disabled?: boolean;
  }>;
  label?: string;
  multiple?: boolean;
  className?: string;
}

export function ChipSelector<T extends string = string>({
  selected,
  onChange,
  options,
  label,
  multiple = true,
  className = ''
}: ChipSelectorProps<T>) {
  const handleChipClick = (value: T) => {
    if (multiple) {
      const isSelected = selected.includes(value);
      if (isSelected) {
        onChange(selected.filter(v => v !== value));
      } else {
        onChange([...selected, value]);
      }
    } else {
      onChange(selected.includes(value) ? [] : [value]);
    }
  };

  return (
    <div className={`space-y-2 ${className}`}>
      {label && (
        <label className="text-sm font-medium text-white/80">
          {label}
        </label>
      )}
      <div className="flex flex-wrap gap-2">
        {options.map((option) => {
          const isSelected = selected.includes(option.value);
          return (
            <button
              key={String(option.value)}
              onClick={() => !option.disabled && handleChipClick(option.value)}
              disabled={option.disabled}
              className={`
                px-3 py-1.5 rounded-full text-sm font-medium
                transition-all duration-200
                ${isSelected
                  ? 'bg-primary-500/30 text-white border-2 border-primary-400'
                  : 'bg-white/10 text-white/70 border-2 border-white/20 hover:bg-white/20'
                }
                ${option.disabled ? 'opacity-50 cursor-not-allowed' : 'cursor-pointer'}
                focus:outline-none focus:ring-2 focus:ring-primary-400/50
              `}
            >
              {option.label}
            </button>
          );
        })}
      </div>
    </div>
  );
}