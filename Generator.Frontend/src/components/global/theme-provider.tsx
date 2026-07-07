import { useEffect } from 'react';
import { useAppSelector } from '@/hooks';

export function ThemeProvider({ children }: { children: React.ReactNode }) {
  const theme = useAppSelector(state => state.theme.activeTheme);

  useEffect(() => {
    const root = window.document.documentElement;
    root.classList.remove('light', 'dark');

    if (theme != 'system') {
      root.classList.add(theme);
      return;
    }

    const systemTheme = window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
    root.classList.add(systemTheme);
  }, [theme]);

  return <>{children}</>;
}
