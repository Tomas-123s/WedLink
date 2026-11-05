import { Component, type ErrorInfo, type ReactNode } from 'react'

interface RenderErrorBoundaryProps {
  children: ReactNode
}

interface RenderErrorBoundaryState {
  error: Error | null
}

export class RenderErrorBoundary extends Component<RenderErrorBoundaryProps, RenderErrorBoundaryState> {
  state: RenderErrorBoundaryState = { error: null }

  static getDerivedStateFromError(error: Error): RenderErrorBoundaryState {
    return { error }
  }

  componentDidCatch(error: Error, info: ErrorInfo) {
    console.error('WedLink render failed:', error, info.componentStack)
  }

  render() {
    if (this.state.error) {
      const message = this.state.error.message || this.state.error.name || 'Unknown rendering error'
      return (
        <main dir="rtl" style={{ maxWidth: 760, margin: '48px auto', padding: 24, color: '#f1e8d8', background: '#242424', fontFamily: 'Tahoma, Arial, sans-serif' }}>
          <h1 style={{ color: '#c6a66b' }}>تعذر عرض الصفحة</h1>
          <p>ظهر خطأ أثناء رسم الصفحة. انسخ تفاصيل الخطأ التالية وأرسلها للمطور:</p>
          <pre dir="ltr" style={{ overflowWrap: 'anywhere', whiteSpace: 'pre-wrap', padding: 16, background: '#151515', border: '1px solid #66583c' }}>{message}</pre>
          {import.meta.env.DEV && this.state.error.stack && <details>
            <summary>تفاصيل إضافية</summary>
            <pre dir="ltr" style={{ overflowWrap: 'anywhere', whiteSpace: 'pre-wrap' }}>{this.state.error.stack}</pre>
          </details>}
        </main>
      )
    }

    return this.props.children
  }
}
