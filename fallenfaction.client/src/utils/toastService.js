// utils/toastService.js - Centralized toast notification system

class ToastService {
  constructor() {
    this.container = null;
    this.toasts = new Map();
    this.nextId = 1;
  }

  init() {
    if (this.container) return;

    this.container = document.createElement('div');
    this.container.id = 'toast-container';
    this.container.className = 'fixed top-4 right-4 z-50 space-y-2 pointer-events-none';
    this.container.setAttribute('aria-live', 'polite');
    this.container.setAttribute('aria-label', 'Notifications');
    document.body.appendChild(this.container);
  }

  show(message, type = 'info', options = {}) {
    this.init();

    const {
      duration = 5000,
      dismissible = true,
      persistent = false,
      action = null
    } = options;

    const id = this.nextId++;
    const toast = this.createToast(id, message, type, { dismissible, action });

    this.container.appendChild(toast);
    this.toasts.set(id, toast);

    // Trigger animation
    requestAnimationFrame(() => {
      toast.classList.remove('translate-x-full', 'opacity-0');
      toast.classList.add('pointer-events-auto');
    });

    // Auto-dismiss if not persistent
    if (!persistent && duration > 0) {
      setTimeout(() => this.dismiss(id), duration);
    }

    return id;
  }

  createToast(id, message, type, { dismissible, action }) {
    const toast = document.createElement('div');

    const colors = {
      success: 'bg-green-500 text-white',
      error: 'bg-red-500 text-white',
      warning: 'bg-amber-500 text-white',
      info: 'bg-blue-500 text-white'
    };

    const icons = {
      success: `<svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z"></path>
      </svg>`,
      error: `<svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-2.5L13.732 4c-.77-.833-1.964-.833-2.732 0L4.732 15.5c-.77.833.192 2.5 1.732 2.5z"></path>
      </svg>`,
      warning: `<svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-2.5L13.732 4c-.77-.833-1.964-.833-2.732 0L4.732 15.5c-.77.833.192 2.5 1.732 2.5z"></path>
      </svg>`,
      info: `<svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"></path>
      </svg>`
    };

    toast.className = `${colors[type]} px-6 py-4 rounded-lg shadow-lg max-w-sm transform transition-all duration-300 translate-x-full opacity-0 flex items-start space-x-3`;
    toast.setAttribute('role', 'alert');
    toast.setAttribute('aria-atomic', 'true');

    let actionButton = '';
    if (action) {
      actionButton = `
        <button onclick="window.toastService.handleAction(${id}, '${action.type}')" 
                class="ml-3 text-current hover:opacity-80 underline text-sm font-medium">
          ${action.label}
        </button>
      `;
    }

    let dismissButton = '';
    if (dismissible) {
      dismissButton = `
        <button onclick="window.toastService.dismiss(${id})" 
                class="ml-3 text-current hover:opacity-80 focus:outline-none focus:ring-2 focus:ring-white/50 rounded p-1"
                aria-label="Dismiss notification">
          <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"></path>
          </svg>
        </button>
      `;
    }

    toast.innerHTML = `
      <div class="flex-shrink-0">
        ${icons[type]}
      </div>
      <div class="flex-1 min-w-0">
        <p class="text-sm font-medium">${message}</p>
      </div>
      <div class="flex items-center">
        ${actionButton}
        ${dismissButton}
      </div>
    `;

    return toast;
  }

  dismiss(id) {
    const toast = this.toasts.get(id);
    if (!toast) return;

    toast.classList.add('translate-x-full', 'opacity-0');
    toast.classList.remove('pointer-events-auto');

    setTimeout(() => {
      if (toast.parentNode) {
        toast.remove();
      }
      this.toasts.delete(id);
    }, 300);
  }

  handleAction(id, actionType) {
    // Emit custom event for action handling
    const event = new CustomEvent('toast-action', {
      detail: { id, actionType }
    });
    document.dispatchEvent(event);
    this.dismiss(id);
  }

  dismissAll() {
    this.toasts.forEach((_, id) => this.dismiss(id));
  }

  // Convenience methods
  success(message, options = {}) {
    return this.show(message, 'success', options);
  }

  error(message, options = {}) {
    return this.show(message, 'error', options);
  }

  warning(message, options = {}) {
    return this.show(message, 'warning', options);
  }

  info(message, options = {}) {
    return this.show(message, 'info', options);
  }
}

// Create global instance
if (typeof window !== 'undefined') {
  window.toastService = new ToastService();
}

export default ToastService;

// Vue 3 Plugin
export const ToastPlugin = {
  install(app) {
    const toastService = new ToastService();

    app.config.globalProperties.$toast = toastService;
    app.provide('toast', toastService);
  }
};

// Composable for Vue 3
export function useToast() {
  const toastService = window.toastService || new ToastService();

  return {
    showToast: toastService.show.bind(toastService),
    success: toastService.success.bind(toastService),
    error: toastService.error.bind(toastService),
    warning: toastService.warning.bind(toastService),
    info: toastService.info.bind(toastService),
    dismiss: toastService.dismiss.bind(toastService),
    dismissAll: toastService.dismissAll.bind(toastService)
  };
}
