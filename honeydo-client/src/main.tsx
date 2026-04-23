import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { BrowserRouter } from 'react-router-dom'
import { MantineProvider, createTheme, localStorageColorSchemeManager } from '@mantine/core'
import type { MantineColorsTuple } from '@mantine/core'
import '@mantine/core/styles.css'
import App from './App'
import './index.css'

// HoneyDo brand palette
const brand: MantineColorsTuple = [
  '#e6fff8', '#b3ffe9', '#80ffd9', '#4dffc9', '#1affb9',
  '#00f5b4', '#00e0ac', '#00c99a', '#00b288', '#009c76',
]
const aqua: MantineColorsTuple = [
  '#e6fdff', '#b3f8ff', '#80f3ff', '#4defff', '#1aeaff',
  '#00e7ff', '#0aebff', '#09d4e6', '#08bdcc', '#07a6b3',
]
const gold: MantineColorsTuple = [
  '#fdf8e1', '#faefc0', '#f7e59e', '#f4dc7d', '#f1d35b',
  '#efcc52', '#eccb53', '#d4b84a', '#bca441', '#a49038',
]
const tangerine: MantineColorsTuple = [
  '#fff4ee', '#ffd9c4', '#ffbe99', '#ffa36f', '#ff8d52',
  '#ff8749', '#ff8547', '#e6773f', '#cc6937', '#b35a2f',
]

const theme = createTheme({
  primaryColor: 'brand',
  colors: { brand, aqua, gold, tangerine },
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
