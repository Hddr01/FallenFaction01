<!-- LoadingSpinner.vue - Reusable loading spinner component -->
<template>
  <div class="spinner-container" :class="{ 'inline': inline }">
    <svg :class="spinnerClasses"
         :style="{ width: size, height: size }"
         fill="none"
         viewBox="0 0 24 24"
         :aria-label="ariaLabel"
         role="status"
         aria-hidden="true">
      <circle class="opacity-25"
              cx="12"
              cy="12"
              r="10"
              stroke="currentColor"
              :stroke-width="strokeWidth">
      </circle>
      <path class="opacity-75"
            fill="currentColor"
            d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z">
      </path>
    </svg>
    <!-- ✅ FIXED: Moved span outside SVG -->
    <span class="sr-only">{{ ariaLabel }}</span>
  </div>
</template>

<script>
  export default {
    name: 'LoadingSpinner',
    props: {
      size: {
        type: String,
        default: '1rem'
      },
      color: {
        type: String,
        default: 'current'
      },
      strokeWidth: {
        type: [String, Number],
        default: '4'
      },
      ariaLabel: {
        type: String,
        default: 'Loading...'
      },
      inline: {
        type: Boolean,
        default: false
      }
    },
    computed: {
      spinnerClasses() {
        const baseClasses = 'animate-spin'
        const colorClass = this.color === 'current' ? 'text-current' : `text-${this.color}`

        return `${baseClasses} ${colorClass}`
      }
    }
  }
</script>

<style scoped>
  .spinner-container {
    display: inline-block;
    position: relative;
  }

    .spinner-container.inline {
      display: inline-flex;
      align-items: center;
    }

  /* Screen reader only text */
  .sr-only {
    position: absolute;
    width: 1px;
    height: 1px;
    padding: 0;
    margin: -1px;
    overflow: hidden;
    clip: rect(0, 0, 0, 0);
    white-space: nowrap;
    border: 0;
  }

  /* Custom animation with better performance */
  @keyframes spin {
    from {
      transform: rotate(0deg);
    }

    to {
      transform: rotate(360deg);
    }
  }

  .animate-spin {
    animation: spin 1s linear infinite;
  }

  /* Respect reduced motion preferences */
  @media (prefers-reduced-motion: reduce) {
    .animate-spin {
      animation: none;
    }

      .animate-spin::after {
        content: '⏳';
        font-size: 1em;
      }
  }

  /* High contrast mode support */
  @media (prefers-contrast: high) {
    .animate-spin {
      filter: contrast(2);
    }
  }
</style>
