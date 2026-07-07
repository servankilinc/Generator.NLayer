import type { ReactNode } from 'react';
import { useCallback, useEffect, useState } from 'react';
import { z } from 'zod';
import { toast } from 'sonner';
import axiosHelper from '@/lib/axios-helper';
import { fetchEntities } from '@/redux/reducers/entitySlice';
import { useAppDispatch } from '@/hooks';
import { useFieldArray, useForm } from 'react-hook-form';
import { PlusCircleIcon, TrashIcon } from 'lucide-react';
import { zodResolver } from '@hookform/resolvers/zod';
import type FieldType from '@/models/fieldType/fieldType';
import type EntityCreateDto from '@/models/entity/entityCreateDto';
import { EntityCreateSchema } from '@/models/entity/entityCreateDto';
import { Button } from '@/components/ui/button';
import { Separator } from '@/components/ui/separator';
import { FieldGroup } from '@/components/ui/field';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import FormInput from '@/components/global/form-input';
import FormCheckbox from '@/components/global/form-checkbox';
import FormCombobox from '@/components/global/form-combobox';
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

type FormData = z.infer<typeof EntityCreateSchema>;

export default function DialogNewEntity({ trigger }: { trigger?: ReactNode } = {}) {
  const dispatch = useAppDispatch();

  const [isOpen, setIsOpen] = useState(false);
  const [fieldTypes, setFieldTypes] = useState<FieldType[]>([]);

  const fetchBaseFieldTypes = useCallback(async () => {
    try {
      const response = await axiosHelper.get<FieldType[]>('/fieldType/list/onbasetype');
      setFieldTypes(response ?? []);
    } catch {
      toast.error('Field Types Could not Readed!');
    }
  }, []);

  useEffect(() => {
    // eslint-disable-next-line react-hooks/set-state-in-effect
    fetchBaseFieldTypes();
  }, [fetchBaseFieldTypes]);

  const createModel: EntityCreateDto = {
    name: '',
    tableName: '',
    softDeletable: true,
    auditable: true,
    archivable: false,
    fields: [
      {
        entityId: 0,
        fieldTypeId: 1,
        name: 'Field',
        isRequired: true,
        isUnique: false,
        isList: false,
        filterable: false
      }
    ]
  };

  const form = useForm<FormData>({
    resolver: zodResolver(EntityCreateSchema),
    defaultValues: createModel
  });

  const { fields, append, remove } = useFieldArray({
    control: form.control,
    name: 'fields'
  });

  async function onSubmit(data: FormData) {
    try {
      await axiosHelper.post('/entity', data);
      toast.success('Entity Created Successfuly');
      setIsOpen(false);
      form.reset();
      dispatch(fetchEntities());
    } catch {
      toast.error('Entity Could not Bee Created!');
    }
  }

  return (
    <Dialog open={isOpen} onOpenChange={setIsOpen} modal={false}>
      <DialogTrigger asChild>
        {trigger ?? (
          <Button variant='destructive' className='w-min m-3'>
            <PlusCircleIcon className='mx-2' /> New Entity
          </Button>
        )}
      </DialogTrigger>
      <DialogContent className='max-w-3xl'>
        <DialogHeader>
          <DialogTitle>Create New Entity</DialogTitle>
          <DialogDescription>Create entity and define fields.</DialogDescription>
        </DialogHeader>
        <form id='form-create-entity' onSubmit={form.handleSubmit(onSubmit)} className='space-y-6'>
          <FieldGroup>
            <FormInput name='name' control={form.control} label='Name' id='txt-entity-name' placeholder='name' autoComplete='off' />
            <FormInput name='tableName' control={form.control} label='Table Name' id='txt-table-name' placeholder='table name' autoComplete='off' />
            <FormCheckbox name='auditable' control={form.control} label='Auditable' id='chbx-auditable' />
            <FormCheckbox name='archivable' control={form.control} label='Archivable' id='chbx-archivable' />
            <FormCheckbox name='softDeletable' control={form.control} label='Soft Deletable' id='chbx-softdeletable' />
          </FieldGroup>

          <Separator />
          {/* Fields */}
          <div className='space-y-4'>
            <div className='flex items-center justify-between'>
              <h3 className='font-semibold'>Fields</h3>
              <Button
                type='button'
                variant='outline'
                onClick={() =>
                  append({
                    entityId: 0,
                    fieldTypeId: 1,
                    name: '',
                    isRequired: false,
                    isUnique: false,
                    isList: false,
                    filterable: false
                  })
                }>
                <PlusCircleIcon className='mr-3' />
                Add Field
              </Button>
            </div>

            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead className='w-[100px]'>Name</TableHead>
                  <TableHead>Type</TableHead>
                  <TableHead>List</TableHead>
                  <TableHead>Unique</TableHead>
                  <TableHead>Required</TableHead>
                  <TableHead>Filterable</TableHead>
                  <TableHead className='text-right'>Actions</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {fields.map((item, index) => (
                  <TableRow key={item.id}>
                    <TableCell className='font-medium'>
                      <FormInput name={`fields.${index}.name`} control={form.control} placeholder='field name' autoComplete='off' />
                    </TableCell>
                    <TableCell className='font-medium'>
                      <FormCombobox name={`fields.${index}.fieldTypeId`} control={form.control} items={fieldTypes} placeholder='Select a field type' />
                    </TableCell>
                    <TableCell className='font-medium'>
                      <FormCheckbox name={`fields.${index}.isList`} control={form.control} className='justify-center' />
                    </TableCell>
                    <TableCell>
                      <FormCheckbox name={`fields.${index}.isUnique`} control={form.control} className='justify-center' />
                    </TableCell>
                    <TableCell>
                      <FormCheckbox name={`fields.${index}.isRequired`} control={form.control} className='justify-center' />
                    </TableCell>
                    <TableCell>
                      <FormCheckbox name={`fields.${index}.filterable`} control={form.control} className='justify-center' />
                    </TableCell>
                    <TableCell className='text-right'>
                      <Button type='button' variant='destructive' onClick={() => remove(index)}>
                        <TrashIcon color='red' />
                      </Button>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </div>
        </form>

        <DialogFooter>
          <DialogClose asChild>
            <Button type='button' variant='outline'>
              Cancel
            </Button>
          </DialogClose>
          <Button type='submit' form='form-create-entity'>
            Save
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
