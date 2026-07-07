import { useAppDispatch, useAppSelector } from '@/hooks';
import axiosHelper from '@/lib/axios-helper';
import type AppSetting from '@/models/appsetting/appsetting';
import { AppSettingUpdateSchema } from '@/models/appsetting/appSettingUpdateDto';
import { setAppSettings } from '@/redux/reducers/projectSlice';
import { zodResolver } from '@hookform/resolvers/zod';
import { useCallback, useEffect, useState } from 'react';
import { Controller, useForm, useWatch } from 'react-hook-form';
import { toast } from 'sonner';
import { z } from 'zod';
import { Button } from '@/components/ui/button';
import { Checkbox } from '@/components/ui/checkbox';
import { FieldError, FieldGroup, FieldLabel } from '@/components/ui/field';
import { Input } from '@/components/ui/input';
import {
  Combobox,
  ComboboxContent,
  ComboboxEmpty,
  ComboboxInput,
  ComboboxItem,
  ComboboxList,
} from '@/components/ui/combobox';
import { Textarea } from '@/components/ui/textarea';
import { FolderOpen, Save, Settings, Database, User, Shield, FolderIcon } from 'lucide-react';
import { fetchEntities } from '@/redux/reducers/entitySlice';

type FormData = z.infer<typeof AppSettingUpdateSchema>;

export default function AppSetting() {
  const dispatch = useAppDispatch();
  const project = useAppSelector(state => state.project.activeProject);
  const entities = useAppSelector(state => state.entity.entities);

  const [loading, setLoading] = useState(false);
  const [pickingFolder, setPickingFolder] = useState(false);

  const form = useForm<FormData>({
    resolver: zodResolver(AppSettingUpdateSchema)
  });

  const isThereUser = useWatch({ control: form.control, name: 'isThereUser' });
  const isThereRole = useWatch({ control: form.control, name: 'isThereRole' });
  const currentPath = useWatch({ control: form.control, name: 'path' });

  const fetchAppSettings = useCallback(async () => {
    try {
      const response = await axiosHelper.get<AppSetting>('/appSetting');
      if (response != null) {
        dispatch(setAppSettings(response));
        form.reset(response);
      }
    } catch {
      toast.error('App Settings could not be loaded!');
    }
  }, [dispatch, form]);

  useEffect(() => {
    if (entities == null || entities.length === 0) {
      dispatch(fetchEntities());
    }
  }, [dispatch, entities]);

  useEffect(() => {
    if (!isThereUser) {
      form.setValue('userEntityId', null);
    }
  }, [isThereUser, form]);

  useEffect(() => {
    if (!isThereRole) {
      form.setValue('roleEntityId', null);
    }
  }, [isThereRole, form]);

  useEffect(() => {
    if (project != null) {
      fetchAppSettings();
    }
  }, [project, fetchAppSettings]);

  const onSubmit = async (data: FormData) => {
    try {
      setLoading(true);
      const response = await axiosHelper.put<AppSetting>('/appSetting', data);
      if (response != null) {
        dispatch(setAppSettings(response));
        toast.success('App Settings updated successfully');
      }
    } catch {
      toast.error('App Settings could not be updated!');
    } finally {
      setLoading(false);
    }
  };

  const handlePickFolder = async () => {
    try {
      setPickingFolder(true);
      // Modern File System Access API - works in Chromium-based browsers/Electron
      if ('showDirectoryPicker' in window) {
        // eslint-disable-next-line @typescript-eslint/no-explicit-any
        const dirHandle = await (window as any).showDirectoryPicker({ mode: 'read' });
        form.setValue('path', dirHandle.name, { shouldValidate: true });
      } else {
        toast.error('Directory picker is not supported in this browser.');
      }
    } catch (err) {
      const error = err as { name?: string };
      // User cancelled - not an error
      if (error?.name !== 'AbortError') {
        toast.error('Could not open folder picker.');
      }
    } finally {
      setPickingFolder(false);
    }
  };

  return (
    <div className='flex flex-1 flex-col gap-6 p-4 pt-0'>
      <div className='rounded-xl border border-border bg-card shadow-sm'>
        {/* Header */}
        <div className='flex items-center gap-3 border-b border-border px-6 py-4'>
          <div className='flex size-9 items-center justify-center rounded-lg bg-primary/10 text-primary'>
            <Settings className='size-5' />
          </div>
          <div>
            <h1 className='text-xl font-semibold leading-tight'>App Settings</h1>
            <p className='text-sm text-muted-foreground'>Configure your project generation settings</p>
          </div>
        </div>

        <form onSubmit={form.handleSubmit(onSubmit)} className='p-6 space-y-8'>
          {/* Project Info */}
          <section className='space-y-4'>
            <h2 className='text-sm font-semibold uppercase tracking-wider text-muted-foreground flex items-center gap-2'>
              <FolderIcon className='size-4' />
              Project Information
            </h2>
            <div className='grid grid-cols-1 md:grid-cols-2 gap-4'>
              {/* Project Name */}
              <FieldGroup>
                <FieldLabel htmlFor='projectName'>Project Name</FieldLabel>
                <Input
                  id='projectName'
                  placeholder='e.g. MyAwesomeApp'
                  {...form.register('projectName')}
                />
                {form.formState.errors.projectName && (
                  <FieldError>{form.formState.errors.projectName.message}</FieldError>
                )}
              </FieldGroup>

              {/* Solution Name */}
              <FieldGroup>
                <FieldLabel htmlFor='solutionName'>Solution Name</FieldLabel>
                <Input
                  id='solutionName'
                  placeholder='e.g. MyAwesomeApp.Solution'
                  {...form.register('solutionName')}
                />
                {form.formState.errors.solutionName && (
                  <FieldError>{form.formState.errors.solutionName.message}</FieldError>
                )}
              </FieldGroup>

              {/* Path */}
              <FieldGroup className='md:col-span-2'>
                <FieldLabel htmlFor='path'>Output Path</FieldLabel>
                <div className='flex gap-2'>
                  <Input
                    id='path'
                    placeholder='Select or type a folder path...'
                    {...form.register('path')}
                    className='flex-1'
                  />
                  <Button
                    type='button'
                    variant='outline'
                    onClick={handlePickFolder}
                    disabled={pickingFolder}
                    className='shrink-0 gap-2'
                  >
                    <FolderOpen className='size-4' />
                    {pickingFolder ? 'Opening…' : 'Browse'}
                  </Button>
                </div>
                {currentPath && (
                  <p className='text-xs text-muted-foreground mt-1 truncate'>
                    📁 {currentPath}
                  </p>
                )}
                {form.formState.errors.path && (
                  <FieldError>{form.formState.errors.path.message}</FieldError>
                )}
              </FieldGroup>

              {/* DB Connection String */}
              <FieldGroup className='md:col-span-2'>
                <FieldLabel htmlFor='dBConnectionString'>
                  <Database className='size-3.5' />
                  Database Connection String
                </FieldLabel>
                <Textarea
                  id='dBConnectionString'
                  placeholder='Server=.;Database=MyDb;Trusted_Connection=True;'
                  {...form.register('dBConnectionString')}
                  className='min-h-[80px] font-mono text-sm'
                />
                {form.formState.errors.dBConnectionString && (
                  <FieldError>{form.formState.errors.dBConnectionString.message}</FieldError>
                )}
              </FieldGroup>
            </div>
          </section>

          {/* Identity / Auth */}
          <section className='space-y-4'>
            <h2 className='text-sm font-semibold uppercase tracking-wider text-muted-foreground flex items-center gap-2'>
              <Shield className='size-4' />
              Authentication &amp; Authorization
            </h2>

            <div className='rounded-lg border border-border divide-y divide-border'>
              {/* Is There Identity */}
              <div className='flex items-center gap-3 px-4 py-3'>
                <Controller
                  name='isThereIdentity'
                  control={form.control}
                  render={({ field }) => (
                    <Checkbox
                      id='isThereIdentity'
                      checked={field.value}
                      onCheckedChange={field.onChange}
                    />
                  )}
                />
                <div className='flex-1'>
                  <FieldLabel htmlFor='isThereIdentity' className='m-0 font-medium cursor-pointer'>
                    Use Identity
                  </FieldLabel>
                  <p className='text-xs text-muted-foreground'>Enable ASP.NET Core Identity scaffolding</p>
                </div>
              </div>

              {/* Is There User */}
              <div className='flex flex-col px-4 py-3 gap-3'>
                <div className='flex items-center gap-3'>
                  <Controller
                    name='isThereUser'
                    control={form.control}
                    render={({ field }) => (
                      <Checkbox
                        id='isThereUser'
                        checked={field.value}
                        onCheckedChange={field.onChange}
                      />
                    )}
                  />
                  <div className='flex-1'>
                    <FieldLabel htmlFor='isThereUser' className='m-0 font-medium cursor-pointer flex items-center gap-1.5'>
                      <User className='size-3.5' />
                      Use User Entity
                    </FieldLabel>
                    <p className='text-xs text-muted-foreground'>Map an entity as the application user</p>
                  </div>
                </div>

                {isThereUser && (
                  <div className='ml-7 space-y-1'>
                    <FieldLabel htmlFor='userEntityId' className='text-xs text-muted-foreground'>
                      Select User Entity
                    </FieldLabel>
                    <Controller
                      name='userEntityId'
                      control={form.control}
                      render={({ field }) => (
                        <Combobox
                          value={entities.find(e => e.id === field.value)?.name ?? ''}
                          onValueChange={val => {
                            const selected = entities.find(e => e.name === val);
                            field.onChange(selected ? selected.id : null);
                          }}
                        >
                          <ComboboxInput placeholder='Search entity...' showClear />
                          <ComboboxContent>
                            <ComboboxEmpty>No entities found</ComboboxEmpty>
                            <ComboboxList>
                              <ComboboxItem value='' onSelect={() => field.onChange(null)}>
                                — None —
                              </ComboboxItem>
                              {entities.map(entity => (
                                <ComboboxItem
                                  key={entity.id}
                                  value={entity.name}
                                >
                                  {entity.name}
                                </ComboboxItem>
                              ))}
                            </ComboboxList>
                          </ComboboxContent>
                        </Combobox>
                      )}
                    />
                    {form.formState.errors.userEntityId && (
                      <FieldError>{form.formState.errors.userEntityId.message}</FieldError>
                    )}
                  </div>
                )}
              </div>

              {/* Is There Role */}
              <div className='flex flex-col px-4 py-3 gap-3'>
                <div className='flex items-center gap-3'>
                  <Controller
                    name='isThereRole'
                    control={form.control}
                    render={({ field }) => (
                      <Checkbox
                        id='isThereRole'
                        checked={field.value}
                        onCheckedChange={field.onChange}
                      />
                    )}
                  />
                  <div className='flex-1'>
                    <FieldLabel htmlFor='isThereRole' className='m-0 font-medium cursor-pointer flex items-center gap-1.5'>
                      <Shield className='size-3.5' />
                      Use Role Entity
                    </FieldLabel>
                    <p className='text-xs text-muted-foreground'>Map an entity as the application role</p>
                  </div>
                </div>

                {isThereRole && (
                  <div className='ml-7 space-y-1'>
                    <FieldLabel htmlFor='roleEntityId' className='text-xs text-muted-foreground'>
                      Select Role Entity
                    </FieldLabel>
                    <Controller
                      name='roleEntityId'
                      control={form.control}
                      render={({ field }) => (
                        <Combobox
                          value={entities.find(e => e.id === field.value)?.name ?? ''}
                          onValueChange={val => {
                            const selected = entities.find(e => e.name === val);
                            field.onChange(selected ? selected.id : null);
                          }}
                        >
                          <ComboboxInput placeholder='Search entity...' showClear />
                          <ComboboxContent>
                            <ComboboxEmpty>No entities found</ComboboxEmpty>
                            <ComboboxList>
                              <ComboboxItem value='' onSelect={() => field.onChange(null)}>
                                — None —
                              </ComboboxItem>
                              {entities.map(entity => (
                                <ComboboxItem
                                  key={entity.id}
                                  value={entity.name}
                                >
                                  {entity.name}
                                </ComboboxItem>
                              ))}
                            </ComboboxList>
                          </ComboboxContent>
                        </Combobox>
                      )}
                    />
                    {form.formState.errors.roleEntityId && (
                      <FieldError>{form.formState.errors.roleEntityId.message}</FieldError>
                    )}
                  </div>
                )}
              </div>
            </div>
          </section>

          {/* Actions */}
          <div className='flex items-center gap-3 border-t border-border pt-6'>
            <Button type='submit' disabled={loading} className='gap-2'>
              <Save className='size-4' />
              {loading ? 'Saving…' : 'Save Changes'}
            </Button>
          </div>
        </form>
      </div>
    </div>
  );
}
