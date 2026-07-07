import { useState } from 'react';
import { z } from 'zod';
import { toast } from 'sonner';
import { PlusCircleIcon } from 'lucide-react';
import { Controller, useForm } from 'react-hook-form';
import axiosHelper from '@/lib/axios-helper';
import { useAppDispatch } from '@/hooks';
import { fetchProjects } from '@/redux/reducers/projectSlice';
import { zodResolver } from '@hookform/resolvers/zod';
import { Field, FieldError, FieldGroup, FieldLabel } from '@/components/ui/field';
import { Input } from '@/components/ui/input';
import { Button } from '@/components/ui/button';
import {
  Dialog,
  DialogClose,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  DialogTrigger
} from '@/components/ui/dialog';

const formSchema = z.object({
  projectName: z.string().min(2, 'Name must be at least 2 characters.')
});
type FormData = z.infer<typeof formSchema>;

export default function DialogNewProject() {
  const dispatch = useAppDispatch();

  const [isOpen, setIsOpen] = useState(false);

  const form = useForm<FormData>({
    resolver: zodResolver(formSchema),
    defaultValues: {
      projectName: 'My App'
    }
  });

  async function onSubmit(data: FormData) {
    try {
      await axiosHelper.post('project', data);
      toast.success('Project Created Successfuly');
      setIsOpen(false);
      form.reset();
      dispatch(fetchProjects());
    } catch {
      toast.error('Project Could not Bee Created!');
    }
  }

  return (
    <Dialog open={isOpen} onOpenChange={setIsOpen}>
      <DialogTrigger asChild>
        <Button variant='destructive' className='w-min'>
          New Project
        </Button>
      </DialogTrigger>
      <DialogContent className='sm:max-w-sm'>
        <DialogHeader>
          <DialogTitle>
            Create New Project <PlusCircleIcon className='mr-3' />{' '}
          </DialogTitle>
          <DialogDescription>Select this project to be active after creating.</DialogDescription>
        </DialogHeader>
        <form id='form-create-project' onSubmit={form.handleSubmit(onSubmit)}>
          <FieldGroup>
            <Controller
              name='projectName'
              control={form.control}
              render={({ field, fieldState }) => (
                <Field data-invalid={fieldState.invalid}>
                  <FieldLabel htmlFor='txt-project-name'>Project Name</FieldLabel>
                  <Input {...field} id='txt-project-name' aria-invalid={fieldState.invalid} placeholder='project name' autoComplete='off' />
                  {fieldState.invalid && <FieldError errors={[fieldState.error]} />}
                </Field>
              )}
            />
          </FieldGroup>
        </form>
        <DialogFooter>
          <DialogClose asChild>
            <Button type='button' variant='outline'>
              Cancel
            </Button>
          </DialogClose>
          <Button type='submit' form='form-create-project'>
            Save
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
