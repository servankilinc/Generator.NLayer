import { useCallback, useEffect, useState } from 'react';
import { Handle, Position } from '@xyflow/react';
import { useFieldArray, useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { toast } from 'sonner';
import { GitBranch, LayoutList, Pen, PlusCircleIcon, SaveIcon, Trash2, TrashIcon } from 'lucide-react';
import { useNavigate } from 'react-router';
import type z from 'zod';

import type Entity from '@/models/entity/entity';
import type FieldType from '@/models/fieldType/fieldType';
import type FieldUpdateDto from '@/models/field/fieldUpdateDto';
import { FieldsUpdateSchema } from '@/models/field/fieldUpdateDto';
import axiosHelper from '@/lib/axios-helper';
import { useAppDispatch } from '@/hooks';
import { fetchEntities } from '@/redux/reducers/entitySlice';
import FormInput from '@/components/global/form-input';
import FormCheckbox from '@/components/global/form-checkbox';
import FormCombobox from '@/components/global/form-combobox';
import DialogUpdateEntity from '@/views/entity/components/dialog-update-entity';
import DialogRelations from '@/views/entity/components/dialog-relations';
import ConfirmDialog from '@/components/global/confirm-dialog';

interface EntityNodeProps {
  data: {
    entity: Entity;
  };
}

type FormData = z.infer<typeof FieldsUpdateSchema>;

export default function EntityNode({ data }: EntityNodeProps) {
  const { entity } = data;
  const navigate = useNavigate();
  const dispatch = useAppDispatch();

  const [fieldList, setFieldList] = useState<FieldUpdateDto[]>([]);
  const [fieldTypes, setFieldTypes] = useState<FieldType[]>([]);
  const [formChanged, setFormChanged] = useState(false);
  const [showDeleteConfirm, setShowDeleteConfirm] = useState(false);

  const form = useForm<FormData>({
    resolver: zodResolver(FieldsUpdateSchema),
    values: { fields: fieldList }
  });

  const { fields, append, remove } = useFieldArray({
    control: form.control,
    name: 'fields'
  });

  const fetchFields = useCallback(async () => {
    try {
      const response = await axiosHelper.get<FieldUpdateDto[]>('/field/list/updateModel', { params: { entityId: entity.id } });
      setFieldList(response ?? []);
    } catch {
      toast.error('Fields could not be loaded!');
    }
  }, [entity.id]);

  const fetchFieldTypes = useCallback(async () => {
    try {
      const response = await axiosHelper.get<FieldType[]>('/fieldType/list/onbasetype', { params: { entityId: entity.id } });
      setFieldTypes(response ?? []);
    } catch {
      toast.error('Field types could not be loaded!');
    }
  }, [entity.id]);

  useEffect(() => {
    // eslint-disable-next-line react-hooks/set-state-in-effect
    fetchFields();
    fetchFieldTypes();
  }, [fetchFields, fetchFieldTypes]);

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

  const handleDelete = async () => {
    try {
      await axiosHelper.delete('/entity', undefined, { params: { id: entity.id } });
      toast.success('Entity deleted successfully');
      dispatch(fetchEntities());
    } catch {
      toast.error('Entity could not be deleted!');
    }
  };

  async function onSubmit(formData: FormData) {
    try {
      await axiosHelper.put('/field/list', formData.fields, { params: { entityId: entity.id } });
      toast.success('Fields updated successfully');
      form.reset();
      setFormChanged(false);
      dispatch(fetchEntities());
    } catch {
      toast.error('Fields could not be updated!');
    }
  }

  return (
    <div className='min-w-[420px] rounded-md border border-border bg-card text-card-foreground shadow-sm'>
      {/* Header with actions */}
      <div className='flex items-center justify-between gap-2 p-3 border-b border-border bg-muted/30'>
        <div className='flex flex-col'>
          <h3 className='font-semibold leading-none tracking-tight text-sm'>{entity.name}</h3>
          <p className='text-[10px] text-muted-foreground mt-0.5'>{entity.tableName}</p>
        </div>
        <div className='flex items-center gap-1 nodrag'>
          <DialogUpdateEntity
            entityId={entity.id}
            trigger={
              <button
                title='Edit Entity'
                className='flex items-center justify-center w-7 h-7 rounded-md text-muted-foreground hover:text-amber-500 hover:bg-amber-500/10 transition-colors'
              >
                <Pen className='w-3.5 h-3.5' />
              </button>
            }
          />
          <DialogRelations
            entityId={entity.id}
            trigger={
              <button
                title='Relations'
                className='flex items-center justify-center w-7 h-7 rounded-md text-muted-foreground hover:text-sky-500 hover:bg-sky-500/10 transition-colors'
              >
                <GitBranch className='w-3.5 h-3.5' />
              </button>
            }
          />
          <button
            onClick={() => setShowDeleteConfirm(true)}
            title='Delete Entity'
            className='flex items-center justify-center w-7 h-7 rounded-md text-muted-foreground hover:text-destructive hover:bg-destructive/10 transition-colors'
          >
            <Trash2 className='w-3.5 h-3.5' />
          </button>
          <button
            onClick={() => navigate(`/diagram/dtos/${entity.id}`)}
            title='View DTOs'
            className='flex items-center justify-center w-7 h-7 rounded-md text-muted-foreground hover:text-primary hover:bg-primary/10 transition-colors'
          >
            <LayoutList className='w-3.5 h-3.5' />
          </button>
        </div>
      </div>

      {/* Fields table */}
      <div className='nodrag nowheel'>
        <form onSubmit={form.handleSubmit(onSubmit)}>
          {/* Table header */}
          <div className='grid grid-cols-[1fr_100px_28px_28px_28px_28px_28px] gap-1 px-3 py-1.5 text-[10px] font-medium text-muted-foreground border-b border-border bg-muted/20'>
            <span>Name</span>
            <span>Type</span>
            <span className='text-center'>L</span>
            <span className='text-center'>U</span>
            <span className='text-center'>R</span>
            <span className='text-center'>F</span>
            <span></span>
          </div>

          {/* Field rows */}
          <div className='flex flex-col'>
            {fields.map((item, index) => (
              <div key={item.id + item.name} className='relative grid grid-cols-[1fr_100px_28px_28px_28px_28px_28px] gap-1 items-center px-3 py-1 border-b border-border/50 text-xs'>
                {/* Left handle */}
                <Handle
                  type='target'
                  position={Position.Left}
                  id={`field-${entity.fields?.[index]?.id ?? item.id}-left`}
                  className='!w-2 !h-2 !bg-primary/60 hover:!bg-primary !border-none !-left-[5px]'
                  style={{ top: '50%' }}
                />

                {/* Name */}
                <FormInput
                  name={`fields.${index}.name`}
                  control={form.control}
                  placeholder='name'
                  autoComplete='off'
                  className='w-full bg-transparent text-xs px-1 py-0.5 rounded border border-transparent focus:border-border focus:outline-none'
                />

                {/* Type combobox */}
                <FormCombobox
                  name={`fields.${index}.fieldTypeId`}
                  control={form.control}
                  items={fieldTypes}
                  placeholder='type'
                  inputClassName='text-xs h-6'
                  onValueChange={() => setFormChanged(true)}
                />

                {/* List */}
                <FormCheckbox
                  name={`fields.${index}.isList`}
                  control={form.control}
                  className='justify-center'
                  onCheckedChange={() => setFormChanged(true)}
                />

                {/* Unique */}
                <FormCheckbox
                  name={`fields.${index}.isUnique`}
                  control={form.control}
                  className='justify-center'
                  onCheckedChange={() => setFormChanged(true)}
                />

                {/* Required */}
                <FormCheckbox
                  name={`fields.${index}.isRequired`}
                  control={form.control}
                  className='justify-center'
                  onCheckedChange={() => setFormChanged(true)}
                />

                {/* Filterable */}
                <FormCheckbox
                  name={`fields.${index}.filterable`}
                  control={form.control}
                  className='justify-center'
                  onCheckedChange={() => setFormChanged(true)}
                />

                {/* Remove */}
                <button
                  type='button'
                  onClick={() => handleRemove(index)}
                  className='flex items-center justify-center w-5 h-5 rounded text-muted-foreground hover:text-destructive hover:bg-destructive/10 transition-colors'
                >
                  <TrashIcon className='w-3 h-3' />
                </button>

                {/* Right handle */}
                <Handle
                  type='source'
                  position={Position.Right}
                  id={`field-${entity.fields?.[index]?.id ?? item.id}-right`}
                  className='!w-2 !h-2 !bg-primary/60 hover:!bg-primary !border-none !-right-[5px]'
                  style={{ top: '50%' }}
                />
              </div>
            ))}
          </div>

          {/* Footer: Add + Save */}
          <div className='flex items-center justify-between px-3 py-2 bg-muted/10'>
            <button
              type='button'
              onClick={handleAppend}
              className='flex items-center gap-1 text-[10px] text-muted-foreground hover:text-foreground transition-colors'
            >
              <PlusCircleIcon className='w-3 h-3' /> Add Field
            </button>
            {formChanged && (
              <button
                type='submit'
                className='flex items-center gap-1 text-[10px] px-2 py-1 rounded bg-emerald-600 hover:bg-emerald-700 text-white transition-colors'
              >
                <SaveIcon className='w-3 h-3' /> Save
              </button>
            )}
          </div>
        </form>
      </div>
      <ConfirmDialog
        open={showDeleteConfirm}
        onOpenChange={setShowDeleteConfirm}
        title={`Delete "${entity.name}"?`}
        description='This will permanently delete this entity and all its fields. This action cannot be undone.'
        confirmLabel='Delete'
        onConfirm={handleDelete}
      />
    </div>
  );
}
