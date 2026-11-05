import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { QueryClientProvider } from '@tanstack/react-query'
import { BrowserRouter } from 'react-router-dom'
import './index.css'
import App from './App.tsx'
import './features/invitations/editorialLuxury.css'
import { queryClient } from './app/queryClient.ts'
import { RenderErrorBoundary } from './app/RenderErrorBoundary.tsx'

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <RenderErrorBoundary>
      <QueryClientProvider client={queryClient}>
        <BrowserRouter>
          <App />
        </BrowserRouter>
      </QueryClientProvider>
    </RenderErrorBoundary>
  </StrictMode>,
)
