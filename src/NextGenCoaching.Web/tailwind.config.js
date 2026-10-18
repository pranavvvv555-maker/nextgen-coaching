/** @type {import('tailwindcss').Config} */
module.exports = {
  content: [
    './Components/**/*.razor',
    './wwwroot/**/*.html',
    './Pages/**/*.cshtml',
    './Components/**/*.{razor,html}',
    './wwwroot/index.html'
  ],
  theme: {
    extend: {
      colors: {
        // Brand
        'brand-primary':       '#4263F5',
        'brand-primary-dark':  '#2946CF',
        'brand-navy':          '#111C32',
        'brand-navy-light':    '#1A2946',
        'brand-navy-dark':     '#111C32',
        'brand-sky-50':        '#EFF6FF',
        'brand-sky-100':       '#DBEAFE',
        'brand-sky-200':       '#93C5FD',
        'brand-indigo':        '#4F46E5',
        'brand-purple':        '#7C3AED',
        'brand-cyan':          '#06B6D4',
        'brand-emerald':       '#10B981',

        // Text
        'text-primary':        '#0F172A',
        'text-secondary':      '#475569',
        'text-muted':          '#64748B',
        'text-disabled':       '#94A3B8',

        // Surfaces
        'surface':             '#FFFFFF',
        'surface-muted':       '#F7F8FB',
        'border-base':         '#E2E8F0',

        // Status
        'status-success':      '#10B981',
        'status-warning':      '#F59E0B',
        'status-danger':       '#EF4444',
        'status-info':         '#3B82F6',
        'status-purple':       '#8B5CF6',
        'status-orange':       '#F97316'
      },
      fontFamily: {
        sans: ['Manrope', 'Inter', 'system-ui', 'sans-serif'],
        display: ['Manrope', 'Inter', 'sans-serif']
      },
      fontSize: {
        xs: ['14px', { lineHeight: '1.55' }],
        sm: ['16px', { lineHeight: '1.6' }],
        base: ['18px', { lineHeight: '1.65' }],
        lg: ['20px', { lineHeight: '1.5' }],
        xl: ['24px', { lineHeight: '1.4' }],
        '2xl': ['30px', { lineHeight: '1.3' }],
        '3xl': ['36px', { lineHeight: '1.25' }],
        '4xl': ['44px', { lineHeight: '1.2' }],
        '5xl': ['56px', { lineHeight: '1.15' }],
        '6xl': ['68px', { lineHeight: '1.1' }],
        'display': ['32px', { lineHeight: '1.2',  fontWeight: '700' }],
        'h2':      ['28px', { lineHeight: '1.3',  fontWeight: '700' }],
        'h3':      ['23px', { lineHeight: '1.4',  fontWeight: '700' }],
        'body':    ['16px', { lineHeight: '1.5'                    }],
        'caption': ['14px', { lineHeight: '1.4'                    }],
        'meta':    ['12px', { lineHeight: '1.4', letterSpacing: '0.05em' }]
      },
      borderRadius: {
        'card': '16px',
        'btn':  '10px',
        'pill': '9999px'
      },
      boxShadow: {
        'card':       '0 1px 3px rgba(15, 23, 42, 0.06), 0 1px 2px rgba(15, 23, 42, 0.04)',
        'card-hover': '0 12px 28px -6px rgba(15, 23, 42, 0.12), 0 8px 10px -6px rgba(15, 23, 42, 0.06)',
        'glow-blue':   '0 0 35px -5px rgba(37, 99, 235, 0.35)',
        'glow-indigo': '0 0 35px -5px rgba(79, 70, 229, 0.35)',
        'glow-purple': '0 0 35px -5px rgba(124, 58, 237, 0.35)',
        'glow-cyan':   '0 0 35px -5px rgba(6, 182, 212, 0.35)',
        'glow-emerald':'0 0 35px -5px rgba(16, 185, 129, 0.35)',
        'glass':       '0 8px 32px 0 rgba(0, 0, 0, 0.12)'
      },
      keyframes: {
        float: {
          '0%, 100%': { transform: 'translateY(0px)' },
          '50%': { transform: 'translateY(-10px)' }
        },
        'float-slow': {
          '0%, 100%': { transform: 'translateY(0px)' },
          '50%': { transform: 'translateY(-6px)' }
        },
        'float-reverse': {
          '0%, 100%': { transform: 'translateY(0px)' },
          '50%': { transform: 'translateY(8px)' }
        },
        'pulse-glow': {
          '0%, 100%': { opacity: '0.6', transform: 'scale(1)' },
          '50%': { opacity: '1', transform: 'scale(1.05)' }
        },
        shimmer: {
          '0%': { backgroundPosition: '-200% 0' },
          '100%': { backgroundPosition: '200% 0' }
        },
        marquee: {
          '0%': { transform: 'translateX(0%)' },
          '100%': { transform: 'translateX(-50%)' }
        },
        blob: {
          '0%': { transform: 'translate(0px, 0px) scale(1)' },
          '33%': { transform: 'translate(30px, -50px) scale(1.1)' },
          '66%': { transform: 'translate(-20px, 20px) scale(0.9)' },
          '100%': { transform: 'translate(0px, 0px) scale(1)' }
        }
      },
      animation: {
        'float': 'float 4s ease-in-out infinite',
        'float-slow': 'float-slow 6s ease-in-out infinite',
        'float-reverse': 'float-reverse 5s ease-in-out infinite',
        'pulse-glow': 'pulse-glow 3s ease-in-out infinite',
        'shimmer': 'shimmer 3s linear infinite',
        'marquee': 'marquee 25s linear infinite',
        'blob': 'blob 10s infinite'
      }
    }
  },
  plugins: []
}


