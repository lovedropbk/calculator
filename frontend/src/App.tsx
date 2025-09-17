import React, { useState, useEffect } from 'react';
import './App.css';
import { QuoteScreen } from './components/QuoteScreen';
import { GetCurrentParameterVersion } from '../wailsjs/go/main/App';

function App() {
  const [parameterVersion, setParameterVersion] = useState<string>('');
  const [lastSyncTime, setLastSyncTime] = useState<string>('');
  const [isOffline, setIsOffline] = useState<boolean>(false);

  useEffect(() => {
    // Fetch current parameter version on mount
    const fetchParameterVersion = async () => {
      try {
        const version = await GetCurrentParameterVersion();
        setParameterVersion(version);
        
        // Check if we're in offline mode
        if (version && version.includes('2025-08')) {
          setIsOffline(true);
        }
      } catch (error) {
        console.error('Failed to fetch parameter version:', error);
        setParameterVersion('Unknown');
        setIsOffline(true);
      }
    };

    fetchParameterVersion();

    // Refresh version periodically (every 5 minutes)
    const interval = setInterval(fetchParameterVersion, 5 * 60 * 1000);

    return () => clearInterval(interval);
  }, []);

  return (
    <div id="app" className="min-h-screen bg-gradient-to-br from-gray-900 via-blue-900 to-gray-900 flex flex-col">
      {/* Offline indicator */}
      {isOffline && (
        <div className="bg-yellow-600/20 border-b border-yellow-600/30 px-4 py-2">
          <div className="container mx-auto flex items-center justify-center space-x-2">
            <svg className="w-4 h-4 text-yellow-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" />
            </svg>
            <span className="text-yellow-100 text-sm">
              Operating in offline mode - using cached parameters (v{parameterVersion})
            </span>
          </div>
        </div>
      )}
      
      <QuoteScreen
        parameterVersion={parameterVersion}
        lastSyncTime={lastSyncTime}
      />
    </div>
  );
}

export default App;
