import * as React from 'react';
import { Link } from 'react-router';
import { ChevronsUpDown, Command, Gamepad2, Home, LifeBuoy, Monitor, Moon, Network, Send, SunIcon, Zap } from 'lucide-react';
import { useAppDispatch, useAppSelector } from '@/hooks';
import { setTheme } from '@/redux/reducers/themeSlice';
import { Avatar, AvatarFallback } from '@/components/ui/avatar';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
  DropdownMenuGroup,
  DropdownMenuLabel
} from '@/components/ui/dropdown-menu';
import {
  Sidebar,
  SidebarContent,
  SidebarFooter,
  SidebarHeader,
  SidebarMenu,
  SidebarMenuButton,
  SidebarMenuItem,
  SidebarGroup,
  SidebarGroupLabel,
  useSidebar
} from '@/components/ui/sidebar';
import { Separator } from '@/components/ui/separator';
import { Field } from '@/components/ui/field';
import { BadgeDanger, BadgeSuccess } from '../ui/badge';

export function AppSidebar({ ...props }: React.ComponentProps<typeof Sidebar>) {
  const activeProject = useAppSelector(state => state.project.activeProject);

  return (
    <Sidebar variant='inset' {...props}>
      <SidebarHeader>
        <SidebarMenu>
          <SidebarMenuItem>
            <SidebarMenuButton size='lg' asChild>
              <a href='#'>
                <div className='flex aspect-square size-8 items-center justify-center rounded-lg bg-sidebar-primary text-sidebar-primary-foreground'>
                  <Command className='size-4' />
                </div>
                <div className='grid flex-1 text-left text-sm leading-tight'>
                  <span className='truncate font-medium'>GENERATOR V2</span>
                  <span className='truncate text-xs'>HEXPORT</span>
                </div>
              </a>
            </SidebarMenuButton>
          </SidebarMenuItem>
        </SidebarMenu>
      </SidebarHeader>
      <SidebarContent>
        <SidebarGroup>
          <SidebarGroupLabel>Platform</SidebarGroupLabel>
          <SidebarMenu>

            <SidebarMenuItem key={'Home'}>
              <SidebarMenuButton asChild size='sm'>
                <Link to='/'>
                  <Home />
                  <span>Home</span>
                </Link>
              </SidebarMenuButton>
            </SidebarMenuItem>

            <SidebarMenuItem key={'Projects'}>
              <SidebarMenuButton asChild size='sm'>
                <Link to='/projects'>
                  <LifeBuoy />
                  <span>Projects</span>
                </Link>
              </SidebarMenuButton>
            </SidebarMenuItem>

            <SidebarMenuItem key={'Entities'}>
              <SidebarMenuButton asChild size='sm'>
                <Link to='/entities'>
                  <Send />
                  <span>Entities</span>
                </Link>
              </SidebarMenuButton>
            </SidebarMenuItem>

            <SidebarMenuItem key={'Diagram'}>
              <SidebarMenuButton asChild size='sm'>
                <Link to='/diagram'>
                  <Network />
                  <span>Diagram</span>
                </Link>
              </SidebarMenuButton>
            </SidebarMenuItem>

            <SidebarMenuItem key={'Generation'}>
              <SidebarMenuButton asChild size='sm'>
                <Link to='/generation'>
                  <Zap />
                  <span>Generation</span>
                </Link>
              </SidebarMenuButton>
            </SidebarMenuItem>

          </SidebarMenu>
        </SidebarGroup>
      </SidebarContent>
      <SidebarFooter>
        <NavThemeSelector />
        <Field className='opacity-50 p-4' orientation='horizontal'>
          <Gamepad2 />
          <Separator orientation='vertical' className='mx-2' />
          {activeProject?.projectName == null ? (
            <BadgeDanger>No Selected Project</BadgeDanger>
          ) : (
            <BadgeSuccess>{activeProject?.projectName}</BadgeSuccess>
          )}
          <span className='text-sm'></span>
        </Field>
      </SidebarFooter>
    </Sidebar>
  );
}

function NavThemeSelector() {
  const dispatch = useAppDispatch();
  const { isMobile } = useSidebar();

  return (
    <SidebarMenu>
      <SidebarMenuItem>
        <DropdownMenu>
          <DropdownMenuTrigger asChild>
            <SidebarMenuButton size='lg' className='data-[state=open]:bg-sidebar-accent data-[state=open]:text-sidebar-accent-foreground'>
              <Avatar className='h-8 w-8 rounded-lg'>
                <AvatarFallback className='rounded-lg'>
                  <Monitor />
                </AvatarFallback>
              </Avatar>
              <div className='grid flex-1 text-left text-sm leading-tight'>
                <span className='truncate font-medium'>Theme</span>
              </div>
              <ChevronsUpDown className='ml-auto size-4' />
            </SidebarMenuButton>
          </DropdownMenuTrigger>
          <DropdownMenuContent
            className='w-(--radix-dropdown-menu-trigger-width) rounded-lg'
            side={isMobile ? 'bottom' : 'right'}
            align='end'
            sideOffset={4}>
            <DropdownMenuLabel className='p-0 font-normal'>
              <div className='flex items-center gap-2 px-1 py-1.5 text-left text-sm'>
                <span className='truncate font-medium'>Theme</span>
              </div>
            </DropdownMenuLabel>
            <DropdownMenuSeparator />
            <DropdownMenuGroup>
              <DropdownMenuItem onSelect={() => dispatch(setTheme('system'))}>
                <Monitor />
                System
              </DropdownMenuItem>
              <DropdownMenuItem onSelect={() => dispatch(setTheme('light'))}>
                <SunIcon />
                Light
              </DropdownMenuItem>
              <DropdownMenuItem onSelect={() => dispatch(setTheme('dark'))}>
                <Moon />
                Dark
              </DropdownMenuItem>
            </DropdownMenuGroup>
          </DropdownMenuContent>
        </DropdownMenu>
      </SidebarMenuItem>
    </SidebarMenu>
  );
}
