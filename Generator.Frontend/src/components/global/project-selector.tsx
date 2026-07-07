import { useEffect, useState } from 'react';
import { useAppDispatch } from '@/hooks';
import axiosHelper from '@/lib/axios-helper';
import type Project from '@/models/project/project';
import { fetchProjects, setActiveProject } from '@/redux/reducers/projectSlice';
import { useAppSelector } from '@/hooks';
import { toast } from 'sonner';
import { z } from 'zod';
import { Controller, useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { Command, FolderOpen, Plus, Loader2 } from 'lucide-react';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Field, FieldError, FieldGroup, FieldLabel } from '@/components/ui/field';
import { Input } from '@/components/ui/input';
import { Skeleton } from '@/components/ui/skeleton';

const formSchema = z.object({
  projectName: z.string().min(2, 'Name must be at least 2 characters.')
});
type FormData = z.infer<typeof formSchema>;

export default function ProjectSelector() {
  const dispatch = useAppDispatch();
  const projects = useAppSelector(state => state.project.projects);
  const isLoading = useAppSelector(state => state.project.loading);
  const [selectingId, setSelectingId] = useState<number | null>(null);
  const [showCreateForm, setShowCreateForm] = useState(false);
  const [isCreating, setIsCreating] = useState(false);

  const form = useForm<FormData>({
    resolver: zodResolver(formSchema),
    defaultValues: { projectName: '' }
  });

  useEffect(() => {
    dispatch(fetchProjects());
  }, [dispatch]);

  const handleSelectProject = async (project: Project) => {
    setSelectingId(project.id);
    try {
      await axiosHelper.post<Project>('/activeProject', undefined, { params: { id: project.id } });
      dispatch(setActiveProject(project));
      toast.success(`Project "${project.projectName}" selected`);
    } catch {
      toast.error('Failed to select project');
    } finally {
      setSelectingId(null);
    }
  };

  const handleCreateProject = async (data: FormData) => {
    setIsCreating(true);
    try {
      await axiosHelper.post('project', data);
      toast.success('Project created successfully');
      form.reset();
      setShowCreateForm(false);
      dispatch(fetchProjects());
    } catch {
      toast.error('Failed to create project');
    } finally {
      setIsCreating(false);
    }
  };

  return (
    <div className='flex min-h-screen items-center justify-center bg-background p-4'>
      <div className='w-full max-w-lg'>
        {/* Logo & Title */}
        <div className='mb-8 text-center'>
          <div className='mx-auto mb-4 flex size-14 items-center justify-center rounded-2xl bg-primary text-primary-foreground shadow-lg'>
            <Command className='size-7' />
          </div>
          <h1 className='text-2xl font-semibold tracking-tight'>Welcome to Generator</h1>
          <p className='mt-2 text-sm text-muted-foreground'>Select a project to get started, or create a new one.</p>
        </div>

        {/* Project List Card */}
        <Card>
          <CardHeader className='border-b'>
            <div className='flex items-center justify-between'>
              <div>
                <CardTitle className='flex items-center gap-2 text-base'>
                  <FolderOpen className='size-4' />
                  Projects
                </CardTitle>
                <CardDescription>Choose an existing project</CardDescription>
              </div>
              <Button
                variant='outline'
                size='sm'
                onClick={() => setShowCreateForm(prev => !prev)}
                id='btn-toggle-create-project'>
                <Plus className='size-4' data-icon='inline-start' />
                New
              </Button>
            </div>
          </CardHeader>
          <CardContent className='pt-4'>
            {/* Create Form */}
            {showCreateForm && (
              <form onSubmit={form.handleSubmit(handleCreateProject)} className='mb-4 rounded-lg border bg-muted/30 p-3'>
                <FieldGroup>
                  <Controller
                    name='projectName'
                    control={form.control}
                    render={({ field, fieldState }) => (
                      <Field data-invalid={fieldState.invalid}>
                        <FieldLabel htmlFor='txt-new-project-name'>Project Name</FieldLabel>
                        <Input
                          {...field}
                          id='txt-new-project-name'
                          aria-invalid={fieldState.invalid}
                          placeholder='Enter project name'
                          autoComplete='off'
                          autoFocus
                        />
                        {fieldState.invalid && <FieldError errors={[fieldState.error]} />}
                      </Field>
                    )}
                  />
                </FieldGroup>
                <div className='mt-3 flex justify-end gap-2'>
                  <Button
                    type='button'
                    variant='ghost'
                    size='sm'
                    onClick={() => {
                      setShowCreateForm(false);
                      form.reset();
                    }}>
                    Cancel
                  </Button>
                  <Button type='submit' size='sm' disabled={isCreating}>
                    {isCreating && <Loader2 className='size-3 animate-spin' data-icon='inline-start' />}
                    Create
                  </Button>
                </div>
              </form>
            )}

            {/* Loading State */}
            {isLoading ? (
              <div className='space-y-2'>
                {[1, 2, 3].map(i => (
                  <Skeleton key={i} className='h-14 w-full rounded-lg' />
                ))}
              </div>
            ) : projects.length === 0 ? (
              /* Empty State */
              <div className='flex flex-col items-center justify-center py-8 text-center'>
                <div className='mb-3 flex size-12 items-center justify-center rounded-xl bg-muted'>
                  <FolderOpen className='size-5 text-muted-foreground' />
                </div>
                <p className='text-sm font-medium'>No projects yet</p>
                <p className='mt-1 text-xs text-muted-foreground'>Create your first project to begin</p>
              </div>
            ) : (
              /* Project List */
              <div className='space-y-1.5'>
                {projects.map(project => (
                  <button
                    key={project.id}
                    id={`btn-select-project-${project.id}`}
                    onClick={() => handleSelectProject(project)}
                    disabled={selectingId !== null}
                    className='flex w-full items-center justify-between rounded-lg border bg-card px-4 py-3 text-left transition-colors hover:bg-muted/60 disabled:opacity-50'>
                    <div className='min-w-0 flex-1'>
                      <p className='truncate text-sm font-medium'>{project.projectName}</p>
                      <p className='mt-0.5 text-xs text-muted-foreground'>{project.createDate}</p>
                    </div>
                    {selectingId === project.id ? (
                      <Loader2 className='ml-3 size-4 shrink-0 animate-spin text-primary' />
                    ) : (
                      <span className='ml-3 text-xs text-muted-foreground'>Select →</span>
                    )}
                  </button>
                ))}
              </div>
            )}
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
