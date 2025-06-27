import { defineConfig } from 'eslint/config'
import globals from 'globals'
import js from '@eslint/js'
import pluginVue from 'eslint-plugin-vue'

export default defineConfig([
  {
    name: 'app/files-to-lint',
    files: ['**/*.{js,mjs,jsx,vue}'],
  },

  {
    name: 'app/files-to-ignore',
    ignores: ['**/dist/**', '**/dist-ssr/**', '**/coverage/**', '**/node_modules/**'],
  },

  {
    languageOptions: {
      globals: {
        ...globals.browser,
        ...globals.node,
      },
    },
  },

  js.configs.recommended,
  ...pluginVue.configs['flat/essential'],

  {
    name: 'app/custom-rules',
    rules: {
      // Relax Vue component naming rules
      'vue/multi-word-component-names': 'off',

      // Allow unused variables (common in development)
      'no-unused-vars': ['warn', {
        'varsIgnorePattern': '^_',
        'argsIgnorePattern': '^_',
        'ignoreRestSiblings': true
      }],

      // Allow unused expressions (for debugging)
      'no-unused-expressions': 'off',

      // Allow empty catch blocks (common in API error handling)
      'no-empty': ['error', { 'allowEmptyCatch': true }],

      // Allow console.log in development
      'no-console': process.env.NODE_ENV === 'production' ? 'warn' : 'off',

      // Allow debugger in development
      'no-debugger': process.env.NODE_ENV === 'production' ? 'error' : 'off',

      // Less strict about try-catch
      'no-useless-catch': 'off',

      // Allow async functions without await
      'require-await': 'off',
    }
  }
]);
