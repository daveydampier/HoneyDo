import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { BrowserRouter } from 'react-router-dom'
import { MantineProvider, createTheme, localStorageColorSchemeManager } from '@mantine/core'
import '@mantine/core/styles.css'
import App from './App'
import './index.css'

const theme = createTheme({
  primaryColor: 'blue',
  fontFamily: 'system-ui, -apple-system, sans-serif',
  defaultRadius: 'md',
  components: {
    Button: { defaultProps: { size: 'sm' } },
    TextInput: { defaultProps: { size: 'sm' } },
    PasswordInput: { defaultProps: { size: 'sm' } },
    Select: { defaultProps: { size: 'sm' } },
  },
})

// Persists the user's preference in localStorage under 'honeydo-color-scheme'.
// Falls back to the OS/browser preference ('auto') when no value is stored.
const colorSchemeManager = localStorageColorSchemeManager({ key: 'honeydo-color-scheme' })

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <MantineProvider
      theme={theme}
      colorSchemeManager={colorSchemeManager}
      defaultColorScheme="auto"
    >
      <BrowserRouter>
        <App />
      </BrowserRouter>
    </MantineProvider>
  </StrictMode>,
)
