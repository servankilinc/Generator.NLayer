import type { ReactNode } from 'react';
import { useCallback, useEffect, useState } from 'react';
import { z } from 'zod';
import { toast } from 'sonner';
import axiosHelper from '@/lib/axios-helper';
import { Pen, SaveIcon } from 'lucide-react';
import { useAppDispatch } from '@/hooks';
import { fetchEntities } from '@/redux/reducers/entitySlice';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import type Dto from '@/models/dto/dto';
import type EntityUpdateDto from '@/models/entity/entityUpdateDto';
import { EntityUpdateSchema } from '@/models/entity/entityUpdateDto';
import { CrudTypeEnums } from '@/models/enums/enums';
import { Button } from '@/components/ui/button';
import { Dialog, DialogClose, DialogContent, DialogFooter, DialogHeader, DialogTitle, DialogTrigger } from '@/components/ui/dialog';
import { FieldGroup } from '@/components/ui/field';
import { Separator } from '@/components/ui/separator';
import FormInput from '@/components/global/form-input';
import FormCheckbox from '@/components/global/form-checkbox';
import FormCombobox from '@/components/global/form-combobox';

export default function DialogUpdateEntity({ trigger, ...props }: { entityId: number; trigger?: ReactNode }) {
  const dispatch = useAppDispatch();

  const [entity, setEntity] = useState<EntityUpdateDto>();
  const [dtos, setDtos] = useState<Dto[]>();
  const [isOpen, setIsOpen] = useState(false);

  const fetchEntity = useCallback(async () => {
    try {
      const response = await axiosHelper.get<EntityUpdateDto>('/entity/updateModel', { params: { entityId: props.entityId } });
      setEntity(response);
    } catch {
      toast.error('Entity Could not Readed!');
    }
  }, [props.entityId]);

  const fetchDtos = useCallback(async () => {
    try {
      const response = await axiosHelper.get<Dto[]>('/dto/list/byEntity', { params: { entityId: props.entityId } });
      setDtos(response);
    } catch {
      toast.error('Dtos Could not Readed!');
    }
  }, [props.entityId]);

  useEffect(() => {
    if (isOpen) {
      // eslint-disable-next-line react-hooks/set-state-in-effect
      fetchEntity();
      fetchDtos();
    }
  }, [isOpen, fetchEntity, fetchDtos]);

  type FormData = z.infer<typeof EntityUpdateSchema>;

  const form = useForm<FormData>({
    resolver: zodResolver(EntityUpdateSchema),
    values: entity
  });

  async function onSubmit(data: FormData) {
    try {
      await axiosHelper.put('/entity', data);
      toast.success('Entity Updated Successfuly');
      dispatch(fetchEntities());
      setIsOpen(false);
      form.reset();
    } catch {
      toast.error('Entity Could not Bee Updated!');
    }
  }

  return (
    <Dialog open={isOpen} onOpenChange={setIsOpen} modal={false}>
      <DialogTrigger asChild>
        {trigger ?? (
          <Button variant='ghost' className='bg-amber-600' size='sm'>
            <Pen className='size-4 mr-2' /> Edit
          </Button>
        )}
      </DialogTrigger>
      <DialogContent className='max-w-3xl'>
        <DialogHeader>
          <DialogTitle>Edit {entity?.name}</DialogTitle>
        </DialogHeader>
        
        <Separator/>

        <form id='form-update-entity' onSubmit={form.handleSubmit(onSubmit)} className='space-y-6'>
          <FieldGroup>
            <FormInput name='name' control={form.control} label='Name' id='txt-entity-name' placeholder='name' autoComplete='off' />
            <FormInput name='tableName' control={form.control} label='Table Name' id='txt-table-name' placeholder='table name' autoComplete='off' />
          </FieldGroup>

          <FieldGroup className='grid grid-cols-1 gap-4 sm:grid-cols-3'>
            <FormCheckbox name='auditable' control={form.control} label='Auditable' id='chbx-auditable' />
            <FormCheckbox name='archivable' control={form.control} label='Archivable' id='chbx-archivable' />
            <FormCheckbox name='softDeletable' control={form.control} label='Soft Deletable' id='chbx-softdeletable' />
          </FieldGroup>

          <FieldGroup className='grid grid-cols-1 gap-4 sm:grid-cols-2'>
            <FormCombobox name='createDtoId' control={form.control} label='Create DTO' id='cmbx-create-dto' items={dtos?.filter(f => f.crudTypeId == CrudTypeEnums.Create)} placeholder='Select a field type' />
            <FormCombobox name='updateDtoId' control={form.control} label='Update DTO' id='cmbx-update-dto' items={dtos?.filter(f => f.crudTypeId == CrudTypeEnums.Update)} placeholder='Select a field type' />
            <FormCombobox name='deleteDtoId' control={form.control} label='Delete DTO' id='cmbx-delete-dto' items={dtos?.filter(f => f.crudTypeId == CrudTypeEnums.Delete)} placeholder='Select a field type' />
            <FormCombobox name='reportDtoId' control={form.control} label='Report DTO' id='cmbx-report-dto' items={dtos?.filter(f => f.crudTypeId == CrudTypeEnums.Read)} placeholder='Select a field type' />
            <FormCombobox name='basicResponseDtoId' control={form.control} label='Basic Response DTO' id='cmbx-basic-response-dto' items={dtos?.filter(f => f.crudTypeId == CrudTypeEnums.Read)} placeholder='Select a field type' />
            <FormCombobox name='detailResponseDtoId' control={form.control} label='Detail Response DTO' id='cmbx-detail-response-dto' items={dtos?.filter(f => f.crudTypeId == CrudTypeEnums.Read)} placeholder='Select a field type' />
          </FieldGroup>
        </form>

        <DialogFooter>
          <DialogClose asChild>
            <Button type='button' variant='outline'>
              Cancel
            </Button>
          </DialogClose>
          <Button type='submit' form='form-update-entity' className='bg-emerald-600 hover:bg-emerald-700 text-white'>
            <SaveIcon className='mr-1 mb-0.5 self-center' />
              Save
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
