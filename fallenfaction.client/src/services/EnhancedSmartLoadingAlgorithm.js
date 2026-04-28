// Enhanced Smart Loading Algorithm
// File: src/services/EnhancedSmartLoadingAlgorithm.js
import { storage } from '../utils/storage.js'

// First, extract your original SmartLoadingAlgorithm class
class SmartLoadingAlgorithm {
  constructor() {
    this.MINIMUM_LOADING_TIME = 300;
    this.MAXIMUM_LOADING_TIME = 5000;
    this.FAST_DEVICE_THRESHOLD = 75;
    this.SLOW_NETWORK_THRESHOLD = 30;
  }

  // Your original methods (copy from App.vue)
  analyzeDeviceCapabilities() {
    const capabilities = {
      cpuCores: navigator.hardwareConcurrency || 4,
      deviceMemory: navigator.deviceMemory || 4,
      screenFactor: this.getScreenFactor(),
      browserScore: this.getBrowserScore(),
      reducedMotion: window.matchMedia('(prefers-reduced-motion: reduce)').matches,
      lowBattery: this.isLowBattery()
    };

    capabilities.score = this.calculateDeviceScore(capabilities);
    return capabilities;
  }

  getScreenFactor() {
    const width = window.innerWidth;
    const height = window.innerHeight;
    const pixels = width * height;
    
    if (pixels > 2073600) return 1;
    if (pixels > 921600) return 0.8;
    if (pixels > 480000) return 0.6;
    return 0.4;
  }

  getBrowserScore() {
    const userAgent = navigator.userAgent;
    
    if (userAgent.includes('Chrome') && this.getChromeVersion() >= 90) return 1;
    if (userAgent.includes('Firefox') && this.getFirefoxVersion() >= 88) return 0.9;
    if (userAgent.includes('Safari') && this.getSafariVersion() >= 14) return 0.9;
    if (userAgent.includes('Edge') && this.getEdgeVersion() >= 90) return 0.95;
    
    return 0.7;
  }

  getChromeVersion() {
    const match = navigator.userAgent.match(/Chrome\/(\d+)/);
    return match ? parseInt(match[1]) : 0;
  }

  getFirefoxVersion() {
    const match = navigator.userAgent.match(/Firefox\/(\d+)/);
    return match ? parseInt(match[1]) : 0;
  }

  getSafariVersion() {
    const match = navigator.userAgent.match(/Version\/(\d+)/);
    return match ? parseInt(match[1]) : 0;
  }

  getEdgeVersion() {
    const match = navigator.userAgent.match(/Edg\/(\d+)/);
    return match ? parseInt(match[1]) : 0;
  }

  async isLowBattery() {
    if ('getBattery' in navigator) {
      try {
        const battery = await navigator.getBattery();
        return battery.level < 0.2 && !battery.charging;
      } catch {
        return false;
      }
    }
    return false;
  }

  calculateDeviceScore(capabilities) {
    let score = 0;
    
    score += Math.min(capabilities.cpuCores / 8, 1) * 25;
    score += Math.min(capabilities.deviceMemory / 8, 1) * 25;
    score += capabilities.screenFactor * 20;
    score += capabilities.browserScore * 20;
    
    if (capabilities.reducedMotion) score -= 5;
    if (capabilities.lowBattery) score -= 10;
    
    score += this.getPerformanceBoost() * 10;
    
    return Math.max(0, Math.min(100, Math.round(score)));
  }

  getPerformanceBoost() {
    const hasServiceWorker = 'serviceWorker' in navigator ? 0.3 : 0;
    const hasWebAssembly = 'WebAssembly' in window ? 0.3 : 0;
    const hasIndexedDB = 'indexedDB' in window ? 0.2 : 0;
    const hasWebWorkers = 'Worker' in window ? 0.2 : 0;
    
    return hasServiceWorker + hasWebAssembly + hasIndexedDB + hasWebWorkers;
  }

  analyzeNetworkInfo() {
    const connection = navigator.connection || navigator.mozConnection || navigator.webkitConnection;
    
    if (!connection) {
      return {
        effectiveType: 'unknown',
        downlink: 10,
        rtt: 100,
        score: 50
      };
    }

    const networkScore = this.calculateNetworkScore(connection);
    
    return {
      effectiveType: connection.effectiveType,
      downlink: connection.downlink,
      rtt: connection.rtt,
      saveData: connection.saveData,
      score: networkScore
    };
  }

  calculateNetworkScore(connection) {
    const typeScores = {
      'slow-2g': 10,
      '2g': 20,
      '3g': 50,
      '4g': 80,
      '5g': 100
    };

    let score = typeScores[connection.effectiveType] || 50;
    
    if (connection.downlink) {
      if (connection.downlink >= 10) score += 10;
      else if (connection.downlink < 1) score -= 20;
    }
    
    if (connection.rtt) {
      if (connection.rtt < 100) score += 5;
      else if (connection.rtt > 300) score -= 15;
    }
    
    if (connection.saveData) score -= 10;
    
    return Math.max(0, Math.min(100, score));
  }

  determineLoadingStrategy(deviceScore, networkScore) {
    const combinedScore = (deviceScore + networkScore) / 2;
    
    if (combinedScore >= 80) {
      return {
        name: 'High Performance',
        timeout: 300,
        type: 'rich',
        gif: '/img/happy_girl.gif',
        text: 'Loading...',
        showAnimations: true,
        preloadExtra: true
      };
    } else if (combinedScore >= 60) {
      return {
        name: 'Standard',
        timeout: 600,
        type: 'standard',
        gif: '/img/happy_girl.gif',
        text: 'Loading...',
        showAnimations: true,
        preloadExtra: false
      };
    } else if (combinedScore >= 40) {
      return {
        name: 'Optimized',
        timeout: 1000,
        type: 'basic',
        gif: '/img/simple_loading.gif',
        text: 'Loading...',
        showAnimations: false,
        preloadExtra: false
      };
    } else {
      return {
        name: 'Low Performance',
        timeout: 1500,
        type: 'minimal',
        gif: '',
        text: 'Loading...',
        showAnimations: false,
        preloadExtra: false
      };
    }
  }

  calculateAdaptiveTimeout(baseTimeout, resourceComplexity = 1) {
    const adjusted = baseTimeout * resourceComplexity;
    return Math.max(this.MINIMUM_LOADING_TIME, Math.min(this.MAXIMUM_LOADING_TIME, adjusted));
  }
}

// Enhanced Smart Loading Algorithm
class EnhancedSmartLoadingAlgorithm extends SmartLoadingAlgorithm {
  constructor(config = {}) {
    super();
    
    // Configurable thresholds
    this.config = {
      MINIMUM_LOADING_TIME: config.minTime || 300,
      MAXIMUM_LOADING_TIME: config.maxTime || 5000,
      FAST_DEVICE_THRESHOLD: config.fastThreshold || 75,
      SLOW_NETWORK_THRESHOLD: config.slowThreshold || 30,
      LEARNING_ENABLED: config.enableLearning || true,
      ...config
    };
    
    // Performance history for machine learning
    this.performanceHistory = this.loadPerformanceHistory();
  }

  // Enhanced browser detection using feature detection
  getBrowserScore() {
    const features = {
      webgl2: !!window.WebGL2RenderingContext,
      webassembly: typeof WebAssembly === 'object',
      serviceWorker: 'serviceWorker' in navigator,
      intersectionObserver: 'IntersectionObserver' in window,
      webWorkers: 'Worker' in window,
      indexedDB: 'indexedDB' in window,
      localStorage: 'localStorage' in window,
      sessionStorage: 'sessionStorage' in window,
      fetch: 'fetch' in window,
      promise: 'Promise' in window,
      asyncAwait: (async () => {})().constructor === Promise,
      es6Modules: 'noModule' in document.createElement('script'),
      css3Support: CSS.supports('display', 'grid'),
      touchSupport: 'ontouchstart' in window,
      devicePixelRatio: window.devicePixelRatio || 1
    };

    // Calculate score based on modern web capabilities
    let score = 0;
    const weights = {
      webgl2: 15,
      webassembly: 15,
      serviceWorker: 10,
      intersectionObserver: 8,
      webWorkers: 10,
      indexedDB: 8,
      localStorage: 5,
      sessionStorage: 3,
      fetch: 8,
      promise: 8,
      asyncAwait: 5,
      es6Modules: 10,
      css3Support: 8,
      touchSupport: 2,
      devicePixelRatio: 5
    };

    Object.entries(features).forEach(([feature, supported]) => {
      if (supported) score += weights[feature] || 0;
    });

    return Math.min(100, score);
  }

  // Machine learning component
  loadPerformanceHistory() {
    return storage.smartLoadingHistory.get() ?? []
  }

  savePerformanceHistory() {
    if (!this.config.LEARNING_ENABLED) return;
    try {
      storage.smartLoadingHistory.set(this.performanceHistory.slice(-50))
    } catch {
      // Silently handle storage errors
    }
  }

  // Learn from actual performance
  recordPerformance(deviceScore, networkScore, predictedTime, actualTime, strategy) {
    if (!this.config.LEARNING_ENABLED) return;

    const performance = {
      timestamp: Date.now(),
      deviceScore,
      networkScore,
      predictedTime,
      actualTime,
      strategy,
      accuracy: Math.abs(predictedTime - actualTime) / predictedTime,
      userAgent: navigator.userAgent,
      viewport: `${window.innerWidth}x${window.innerHeight}`
    };

    this.performanceHistory.push(performance);
    this.savePerformanceHistory();
  }

  // Enhanced strategy determination with AGGRESSIVE high-performance bypass
  determineLoadingStrategy(deviceScore, networkScore) {
    // 🚀 AGGRESSIVE BYPASS for excellent devices
    const combinedScore = (deviceScore + (networkScore || 75)) / 2;
    const isExcellentDevice = deviceScore >= 95 && (networkScore || 75) >= 85;
    
    if (isExcellentDevice) {
      return {
        name: 'Ultra High Performance (Bypass)',
        timeout: 150, // Minimal for smooth UX only
        type: 'rich',
        gif: '/img/happy_girl.gif',
        text: 'Loading...',
        showAnimations: true,
        preloadExtra: false, // Skip to avoid delays
        bypass: true // Flag for total bypass
      };
    }
    
    // For non-excellent devices, use the normal ML logic
    const baseStrategy = super.determineLoadingStrategy(deviceScore, networkScore || 75);
    
    if (!this.config.LEARNING_ENABLED || this.performanceHistory.length < 5) {
      return baseStrategy;
    }

    // Find similar past scenarios
    const similarScenarios = this.performanceHistory.filter(entry => {
      const deviceDiff = Math.abs(entry.deviceScore - deviceScore);
      const networkDiff = Math.abs(entry.networkScore - (networkScore || 75));
      return deviceDiff <= 10 && networkDiff <= 15;
    });

    if (similarScenarios.length >= 3) {
      // Adjust timeout based on historical performance
      const avgActualTime = similarScenarios.reduce((sum, entry) => 
        sum + entry.actualTime, 0) / similarScenarios.length;
      
      const avgAccuracy = similarScenarios.reduce((sum, entry) => 
        sum + entry.accuracy, 0) / similarScenarios.length;

      // If we're consistently over/under-predicting, adjust
      if (avgAccuracy > 0.3) {
        const adjustment = avgActualTime / baseStrategy.timeout;
        baseStrategy.timeout = Math.round(baseStrategy.timeout * adjustment);
        baseStrategy.timeout = Math.max(
          this.config.MINIMUM_LOADING_TIME, 
          Math.min(this.config.MAXIMUM_LOADING_TIME, baseStrategy.timeout)
        );
        baseStrategy.name += ' (ML-Adjusted)';
      }
    }

    return baseStrategy;
  }

  // Enhanced network analysis with more metrics
  analyzeNetworkInfo() {
    const baseInfo = super.analyzeNetworkInfo();
    
    // Ensure score is always defined
    if (baseInfo.score === undefined || isNaN(baseInfo.score)) {
      baseInfo.score = 75; // Default to good performance
    }
    
    // Add performance timing analysis
    if (performance.timing && performance.timing.navigationStart) {
      const timing = performance.timing;
      const navigationStart = timing.navigationStart;
      const responseEnd = timing.responseEnd;
      const domComplete = timing.domComplete;
      
      if (responseEnd && navigationStart) {
        baseInfo.pageLoadTime = responseEnd - navigationStart;
      }
      if (domComplete && responseEnd) {
        baseInfo.domProcessingTime = domComplete - responseEnd;
      }
    }

    // Add connection stability score
    if (navigator.connection) {
      const conn = navigator.connection;
      baseInfo.stability = this.calculateConnectionStability(conn);
    }

    return baseInfo;
  }

  calculateConnectionStability(connection) {
    // Simple stability heuristic based on connection type and RTT
    const typeStability = {
      'slow-2g': 0.3,
      '2g': 0.4,
      '3g': 0.6,
      '4g': 0.8,
      '5g': 0.95
    };

    let stability = typeStability[connection.effectiveType] || 0.5;
    
    // Adjust based on RTT variability
    if (connection.rtt) {
      if (connection.rtt < 50) stability += 0.1;
      else if (connection.rtt > 500) stability -= 0.2;
    }

    return Math.max(0, Math.min(1, stability));
  }

  // Resource preloading strategy
  async preloadCriticalResources(strategy) {
    if (!strategy.preloadExtra) return;

    // Only preload resources that actually exist in your project
    const criticalResources = [
      // Add your actual resources here
      { type: 'image', url: '/img/happy_girl.gif' },
      // { type: 'font', url: '/fonts/your-font.woff2' }, // custom fonts
      // { type: 'script', url: '/js/your-script.js' }, // critical JS
    ];

    // Skip preloading if no resources are configured
    if (criticalResources.length === 0) {
      return;
    }

    const preloadPromises = criticalResources.map(resource => 
      this.preloadResource(resource)
    );

    try {
      await Promise.allSettled(preloadPromises);
    } catch (error) {
      // Silently handle preloading errors
    }
  }

  preloadResource({ type, url }) {
    return new Promise((resolve, reject) => {
      const link = document.createElement('link');
      link.rel = 'preload';
      link.href = url;
      link.as = type;
      link.onload = resolve;
      link.onerror = reject;
      document.head.appendChild(link);
      
      // Cleanup after 5 seconds
      setTimeout(() => {
        if (link.parentNode) {
          link.parentNode.removeChild(link);
        }
        resolve();
      }, 5000);
    });
  }

  // Performance analytics
  getAnalytics() {
    if (this.performanceHistory.length === 0) return null;

    const recent = this.performanceHistory.slice(-20);
    
    return {
      totalLoads: this.performanceHistory.length,
      averageAccuracy: recent.reduce((sum, entry) => sum + (1 - entry.accuracy), 0) / recent.length,
      averageLoadTime: recent.reduce((sum, entry) => sum + entry.actualTime, 0) / recent.length,
      mostCommonStrategy: this.getMostCommonStrategy(recent),
      performanceTrend: this.calculatePerformanceTrend(recent)
    };
  }

  getMostCommonStrategy(entries) {
    const strategies = {};
    entries.forEach(entry => {
      strategies[entry.strategy] = (strategies[entry.strategy] || 0) + 1;
    });
    
    return Object.entries(strategies)
      .sort(([,a], [,b]) => b - a)[0]?.[0] || 'Unknown';
  }

  calculatePerformanceTrend(entries) {
    if (entries.length < 5) return 'insufficient_data';
    
    const first = entries.slice(0, Math.floor(entries.length / 2));
    const last = entries.slice(Math.floor(entries.length / 2));
    
    const firstAvg = first.reduce((sum, entry) => sum + entry.actualTime, 0) / first.length;
    const lastAvg = last.reduce((sum, entry) => sum + entry.actualTime, 0) / last.length;
    
    const improvement = (firstAvg - lastAvg) / firstAvg;
    
    if (improvement > 0.1) return 'improving';
    if (improvement < -0.1) return 'degrading';
    return 'stable';
  }
}

export { SmartLoadingAlgorithm, EnhancedSmartLoadingAlgorithm };
export default EnhancedSmartLoadingAlgorithm;