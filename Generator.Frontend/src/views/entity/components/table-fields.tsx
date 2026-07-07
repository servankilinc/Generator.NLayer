import { useCallback, useEffect, useState } from 'react';
import { PlusCircleIcon, SaveIcon, TrashIcon } from 'lucide-react';
import axiosHelper from '@/lib/axios-helper';
import { toast } from 'sonner';
import z from 'zod';
import { useFieldArray, useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import type FieldType from '@/models/fieldType/fieldType';
import { FieldsUpdateSchema } from '@/models/field/fieldUpdateDto';
import type FieldUpdateDto from '@/models/field/fieldUpdateDto';
import { Button } from '@/components/ui/button';
import { Table, TableBody, TableCell, TableFooter, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import FormInput from '@/components/global/form-input';
import FormCheckbox from '@/components/global/form-checkbox';
import FormCombobox from '@/components/global/form-combobox';

export default function TableFields(props: { entityId: number }) {
  const [fieldList, setFieldsList] = useState<FieldUpdateDto[]>([]);
  const [fieldTypes, setFieldTypes] = useState<FieldType[]>([]);
  const [formChanged, setFormChanged] = useState(false);

  const fetchFields = useCallback(async () => {
    try {
      const response = await axiosHelper.get<FieldUpdateDto[]>('/field/list/updateModel', { params: { entityId: props.entityId } });
      setFieldsList(response ?? []);
    } catch {
      toast.error('Fields Could not Readed!');
    }
  }, [props.entityId]);

  const fetchBaseFieldTypes = useCallback(async () => {
    try {
      const response = await axiosHelper.get<FieldType[]>('/fieldType/list/onbasetype', { params: { entityId: props.entityId } });
      setFieldTypes(response ?? []);
    } catch {
      toast.error('Field Types Could not Readed!');
    }
  }, [props.entityId]);

  useEffect(() => {
    // eslint-disable-next-line react-hooks/set-state-in-effect
    fetchBaseFieldTypes();
    fetchFields();
  }, [fetchBaseFieldTypes, fetchFields]);

  type FormData = z.infer<typeof FieldsUpdateSchema>;
  const form = useForm<FormData>({
    resolver: zodResolver(FieldsUpdateSchema),
    values: {
      fields: fieldList
    }
  });

  const { fields, append, remove } = useFieldArray({
    control: form.control,
    name: 'fields'
  });

  const handleAppend = () => {
    append({
      id: 0,
      fieldTypeId: 1,
      name: '',
      isRequired: false,
      isUnique: false,
      isList: false,
      filterable: false
    });
    setFormChanged(true);
  };

  const handleRemove = (index: number) => {
    remove(index);
    setFormChanged(true);
  };

  async function onSubmit(data: FormData) {
    try {
      await axiosHelper.put('/field/list', data.fields, { params: { entityId: props.entityId } });
      toast.success('Fields Updated Successfuly');
      form.reset();
      setFormChanged(false);
    } catch {
      toast.error('Fields Could not Bee Updated!');
    }
  }

  return (
    <div className='space-y-4'>
      <form id='form-update-fields' onSubmit={form.handleSubmit(onSubmit)} onChange={() => setFormChanged(true)} className='space-y-6'>
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Name</TableHead>
              <TableHead>Type</TableHead>
              <TableHead>List</TableHead>
              <TableHead>Unique</TableHead>
              <TableHead>Required</TableHead>
              <TableHead>Filterable</TableHead>
              <TableHead className='text-right'>Actions</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {fields != null &&
              fields.map((item, index) => (
                <TableRow key={item.id + item.name}>
                  <TableCell className='font-medium'>
                    <FormInput name={`fields.${index}.name`} control={form.control} placeholder='field name' autoComplete='off' fieldClassName='w-28' />
                  </TableCell>
                  <TableCell className='font-medium'>
                    <FormCombobox name={`fields.${index}.fieldTypeId`} control={form.control} items={fieldTypes} placeholder='Select a field type' fieldClassName='w-28' />
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
                    <Button type='button' variant='destructive' onClick={() => handleRemove(index)}>
                      <TrashIcon color='red' />
                    </Button>
                  </TableCell>
                </TableRow>
              ))}
              <TableRow >
                <TableCell colSpan={7} className='text-center text-sm text-gray-500'>
                  <Button type='button' size='sm' variant='ghost' onClick={handleAppend} className='my-1 bg-gray-700'>
                    <PlusCircleIcon className='mr-3' /> Add Field
                  </Button>
                </TableCell>
            </TableRow>
          </TableBody>
          {formChanged && (
            <TableFooter>
              <TableRow>
                <TableCell colSpan={7}>
                  <Button type='submit' form='form-update-fields' className='mt-3 float-right bg-green-500 hover:bg-green-600 text-white'>
                    <SaveIcon /> Save Changes
                  </Button>
                </TableCell>
              </TableRow>
            </TableFooter>
          )}
        </Table>
      </form>
    </div>
  );
}
