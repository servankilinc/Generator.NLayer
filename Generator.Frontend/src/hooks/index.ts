import * as React from 'react';
import { useDispatch, useSelector } from 'react-redux';
import type { RootState, AppDispatch } from '@/redux/store';

//#region Redux Hooks
// Use throughout your app instead of plain `useDispatch` and `useSelector`
export const useAppDispatch = useDispatch.withTypes<AppDispatch>();
export const useAppSelector = useSelector.withTypes<RootState>();
//#endregion

//#region Mobile Hook
const MOBILE_BREAKPOINT = 768;

export const useIsMobile = () => {
  const [isMobile, setIsMobile] = React.useState<boolean>(
    typeof window !== 'undefined' ? window.innerWidth < MOBILE_BREAKPOINT : false
  );

  React.useEffect(() => {
    const mql = window.matchMedia(`(max-width: ${MOBILE_BREAKPOINT - 1}px)`);
    const onChange = () => {
      setIsMobile(window.innerWidth < MOBILE_BREAKPOINT);
    };
    mql.addEventListener('change', onChange);
    return () => mql.removeEventListener('change', onChange);
  }, []);

  return isMobile;
};
//#endregion
