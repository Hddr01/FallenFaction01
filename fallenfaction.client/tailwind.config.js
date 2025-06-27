/** @type {import('tailwindcss').Config} */
export default {
  content: [
    "./src/**/*.{vue,js,ts,jsx,tsx}",
  ],
  theme: {
    extend: {
      colors: {
        // Map your CSS variables to Tailwind color names
        'app': {
          'background': 'var(--color-background)',
          'background-soft': 'var(--color-background-soft)',
          'background-mute': 'var(--color-background-mute)',
          'border': 'var(--color-border)',
          'border-hover': 'var(--color-border-hover)',
          'heading': 'var(--color-heading)',
          'text': 'var(--color-text)',
        },
        'theme': {
          'white': 'var(--vt-c-white)',
          'white-soft': 'var(--vt-c-white-soft)',
          'white-mute': 'var(--vt-c-white-mute)',
          'black': 'var(--vt-c-black)',
          'black-soft': 'var(--vt-c-black-soft)',
          'black-mute': 'var(--vt-c-black-mute)',
          'indigo': 'var(--vt-c-indigo)',
        }
      },
      spacing: {
        'section': 'var(--section-gap)'
      },
      fontFamily: {
        'app': [
          'Inter',
          '-apple-system',
          'BlinkMacSystemFont',
          '"Segoe UI"',
          'Roboto',
          'Oxygen',
          'Ubuntu',
          'Cantarell',
          '"Fira Sans"',
          '"Droid Sans"',
          '"Helvetica Neue"',
          'sans-serif'
        ]
      }
    },
  },
  plugins: [],
}
