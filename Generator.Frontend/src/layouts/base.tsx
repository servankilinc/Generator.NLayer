import { SidebarInset, SidebarProvider, SidebarTrigger } from '@/components/ui/sidebar';
import { Separator } from '@/components/ui/separator';
import { AppSidebar } from '@/components/global/app-sidebar';
import AppBreadcrumb from '@/components/global/app-breadcrumb';
import { ThemeProvider } from '@/components/global/theme-provider';
import ProjectSelector from '@/components/global/project-selector';
import AiChat from '@/components/global/ai-chat';
import { Outlet } from 'react-router';
import { Toaster } from '@/components/ui/sonner';
import { useCallback, useEffect, useState } from 'react';
import { useAppDispatch, useAppSelector } from '@/hooks';
import axiosHelper from '@/lib/axios-helper';
import type Project from '@/models/project/project';
import { setActiveProject } from '@/redux/reducers/projectSlice';

export default function Layout() {
  const dispatch = useAppDispatch();
  const activeProject = useAppSelector(state => state.project.activeProject);
  const [initialLoading, setInitialLoading] = useState(true);

  const fetchActiveProject = useCallback(async () => {
    try {
      const response = await axiosHelper.get<Project>('/activeProject');
      if (response != null) dispatch(setActiveProject(response));
    } catch (error) {
      console.error('Failed to fetch active project:', error);
    } finally {
      setInitialLoading(false);
    }
  }, [dispatch]);

  useEffect(() => {
    fetchActiveProject();
  }, [fetchActiveProject]);

  // Show nothing while loading initial project check
  if (initialLoading) {
    return (
      <ThemeProvider>
        <div className='flex min-h-screen items-center justify-center'>
          <div className='size-6 animate-spin rounded-full border-2 border-primary border-t-transparent' />
        </div>
        <Toaster />
      </ThemeProvider>
    );
  }

  // No active project — show project selector intro page
  if (!activeProject) {
    return (
      <ThemeProvider>
        <ProjectSelector />
        <Toaster />
      </ThemeProvider>
    );
  }

  return (
    <ThemeProvider>
      <SidebarProvider>
        <AppSidebar />
        <SidebarInset>
          <main>
            <header className='flex h-16 shrink-0 items-center gap-2'>
              <div className='flex items-center gap-2 px-4'>
                <SidebarTrigger className='-ml-1' />
                <Separator orientation='vertical' className='mr-2 data-[orientation=vertical]:h-4' />
                <AppBreadcrumb page='current page' links={[{ href: '/', label: 'Home' }]} />
              </div>
            </header>
            <Outlet />
            <Toaster />
            <AiChat />
          </main>
        </SidebarInset>
      </SidebarProvider>
    </ThemeProvider>
  );
}
