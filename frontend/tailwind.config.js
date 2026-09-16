/** @type {import('tailwindcss').Config} */
export default { content: ['./index.html', './src/**/*.{js,ts,jsx,tsx}'], theme: { extend: { fontFamily: { sans: ['Inter', 'ui-sans-serif', 'system-ui'] }, colors: { ink: '#07101f', mint: '#55e6b1' }, boxShadow: { glow: '0 0 40px rgba(85,230,177,.12)' } } }, plugins: [] }
