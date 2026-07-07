import type { ReactNode } from 'react';
import { useCallback, useEffect, useMemo, useState } from 'react';
import { z } from 'zod';
import { toast } from 'sonner';
import axiosHelper from '@/lib/axios-helper';
import { fetchEntities } from '@/redux/reducers/entitySlice';
import { useAppDispatch, useAppSelector } from '@/hooks';
import { useForm, useWatch } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { GitBranch, PencilIcon, PlusCircleIcon, SaveIcon, TrashIcon, XIcon } from 'lucide-react';
import type FieldModel from '@/models/field/field';
import { RelationCreateSchema } from '@/models/relation/relationCreateDto';
import type RelationDetailModel from '@/models/relation/relationDetailModel';
import type RelationType from '@/models/relationType/relationType';
import type DeleteBehaviorType from '@/models/deleteBehaviorType/deleteBehaviorType';
import { Button } from '@/components/ui/button';
import { Dialog, DialogClose, DialogContent, DialogFooter, DialogHeader, DialogTitle, DialogTrigger } from '@/components/ui/dialog';
import { FieldGroup } from '@/components/ui/field';
import ConfirmDialog from '@/components/global/confirm-dialog';
import FormInput from '@/components/global/form-input';
import FormCombobox from '@/components/global/form-combobox';
import { Separator } from '@/components/ui/separator';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';

const formSchema = RelationCreateSchema.extend({
  id: z.number().optional()
});

type RelationFormData = z.infer<typeof formSchema>;
type FormMode = 'create' | 'update';

export default function DialogRelations({ entityId, trigger }: { entityId: number; trigger?: ReactNode }) {
  const dispatch = useAppDispatch();
  const entities = useAppSelector(state => state.entity.entities);

  const [isOpen, setIsOpen] = useState(false);
  const [relations, setRelations] = useState<RelationDetailModel[]>([]);
  const [relationTypes, setRelationTypes] = useState<RelationType[]>([]);
  const [deleteBehaviorTypes, setDeleteBehaviorTypes] = useState<DeleteBehaviorType[]>([]);
  const [fieldMap, setFieldMap] = useState<Record<number, FieldModel[]>>({});
  const [formMode, setFormMode] = useState<FormMode>('create');
  const [relationToDelete, setRelationToDelete] = useState<number | null>(null);

  const defaultValues = useMemo(() => ({
    primaryEntityId: entityId,
    primaryFieldId: 0,
    foreignEntityId: 0,
    foreignFieldId: 0,
    primaryEntityVirPropName: '',
    foreignEntityVirPropName: '',
    relationTypeId: 0,
    deleteBehaviorTypeId: 0
  }), [entityId]);

  const form = useForm<RelationFormData>({
    resolver: zodResolver(formSchema),
    defaultValues
  });

  const primaryEntityId = useWatch({ control: form.control, name: 'primaryEntityId' });
  const foreignEntityId = useWatch({ control: form.control, name: 'foreignEntityId' });

  const primaryFields = fieldMap[primaryEntityId] ?? [];
  const foreignFields = fieldMap[foreignEntityId] ?? [];

  const entityFields = useMemo(() => entities.flatMap(entity => entity.fields ?? []), [entities]);

  const getEntityName = (sourceEntityId: number) => entities.find(entity => entity.id === sourceEntityId)?.name ?? '';

  const getFieldEntityId = useCallback(
    (fieldId: number) => entityFields.find(field => field.id === fieldId)?.entityId ?? 0,
    [entityFields]
  );

  const getFieldEntityName = useCallback(
    (fieldId: number) => {
      const entId = getFieldEntityId(fieldId);
      return entities.find(entity => entity.id === entId)?.name ?? '';
    },
    [entities, getFieldEntityId]
  );

  const fetchRelations = useCallback(async () => {
    try {
      const response = await axiosHelper.get<RelationDetailModel[]>('/relation/list/byEntity', { params: { entityId } });
      setRelations(response ?? []);
    } catch {
      toast.error('Relations Could not Readed!');
    }
  }, [entityId]);

  const fetchRelationTypes = useCallback(async () => {
    try {
      const response = await axiosHelper.get<RelationType[]>('/relationType/list');
      setRelationTypes(response ?? []);
    } catch {
      toast.error('Relation Types Could not Readed!');
    }
  }, []);

  const fetchDeleteBehaviorTypes = useCallback(async () => {
    try {
      const response = await axiosHelper.get<DeleteBehaviorType[]>('/deleteBehaviorType/list');
      setDeleteBehaviorTypes(response ?? []);
    } catch {
      toast.error('Delete Behavior Types Could not Readed!');
    }
  }, []);

  const fetchFieldsByEntity = useCallback(
    async (sourceEntityId: number) => {
      if (sourceEntityId === 0 || fieldMap[sourceEntityId] != null) {
        return;
      }

      try {
        const response = await axiosHelper.get<FieldModel[]>('/field/list/byEntity', { params: { entityId: sourceEntityId } });
        setFieldMap(current => ({ ...current, [sourceEntityId]: response ?? [] }));
      } catch {
        toast.error('Fields Could not Readed!');
      }
    },
    [fieldMap]
  );

  const fetchFieldsForEntityDirect = useCallback(async (sourceEntityId: number) => {
    if (sourceEntityId === 0) return;
    try {
      const response = await axiosHelper.get<FieldModel[]>('/field/list/byEntity', { params: { entityId: sourceEntityId } });
      setFieldMap(current => ({ ...current, [sourceEntityId]: response ?? [] }));
    } catch {
      toast.error('Fields Could not Readed!');
    }
  }, []);

  const resetCreateForm = useCallback(() => {
    setFormMode('create');
    form.reset(defaultValues);
    fetchFieldsByEntity(entityId);
  }, [entityId, fetchFieldsByEntity, form, defaultValues]);

  useEffect(() => {
    if (!isOpen) {
      return;
    }

    // eslint-disable-next-line react-hooks/set-state-in-effect
    fetchRelations();
    fetchRelationTypes();
    fetchDeleteBehaviorTypes();
    fetchFieldsByEntity(entityId);

    if (entities.length === 0) {
      dispatch(fetchEntities());
    }
  }, [dispatch, entities.length, entityId, fetchDeleteBehaviorTypes, fetchFieldsByEntity, fetchRelationTypes, fetchRelations, isOpen]);

  const handlePrimaryEntityChange = (selectedEntityId: number) => {
    form.setValue('primaryEntityId', selectedEntityId, { shouldValidate: true });
    form.setValue('primaryFieldId', 0, { shouldValidate: true });
    fetchFieldsForEntityDirect(selectedEntityId);
  };

  const handleForeignEntityChange = (selectedEntityId: number) => {
    form.setValue('foreignEntityId', selectedEntityId, { shouldValidate: true });
    form.setValue('foreignFieldId', 0, { shouldValidate: true });
    fetchFieldsForEntityDirect(selectedEntityId);
  };

  const handleEdit = async (relation: RelationDetailModel) => {
    const nextPrimaryEntityId = getFieldEntityId(relation.primaryFieldId);
    const nextForeignEntityId = getFieldEntityId(relation.foreignFieldId);

    setFormMode('update');

    if (nextPrimaryEntityId > 0) await fetchFieldsForEntityDirect(nextPrimaryEntityId);
    if (nextForeignEntityId > 0) await fetchFieldsForEntityDirect(nextForeignEntityId);

    form.reset({
      ...relation,
      primaryEntityId: nextPrimaryEntityId,
      foreignEntityId: nextForeignEntityId,
      primaryEntityVirPropName: relation.primaryEntityVirPropName ?? '',
      foreignEntityVirPropName: relation.foreignEntityVirPropName ?? ''
    });
  };

  const handleDelete = async (relationId: number) => {
    try {
      await axiosHelper.delete('/relation', undefined, { params: { relationId } });
      toast.success('Relation Deleted Successfully');
      await fetchRelations();
      if (form.getValues('id') === relationId) {
        resetCreateForm();
      }
    } catch {
      toast.error('Relation Could not Be Deleted!');
    }
  };

  async function onSubmit(data: RelationFormData) {
    const payload = {
      ...data,
      primaryEntityVirPropName: data.primaryEntityVirPropName || null,
      foreignEntityVirPropName: data.foreignEntityVirPropName || null
    };

    try {
      if (formMode === 'update') {
        await axiosHelper.put('/relation', payload);
        toast.success('Relation Updated Successfully');
      } else {
        await axiosHelper.post('/relation', payload);
        toast.success('Relation Created Successfully');
      }

      await fetchRelations();
      resetCreateForm();
    } catch {
      toast.error(formMode === 'update' ? 'Relation Could not Be Updated!' : 'Relation Could not Be Created!');
    }
  }

  return (
    <Dialog open={isOpen} onOpenChange={setIsOpen} modal={false}>
      <DialogTrigger asChild>
        {trigger ?? (
          <Button variant='ghost' className='bg-sky-700' size='sm'>
            <GitBranch className='size-4 mr-2' /> Relations
          </Button>
        )}
      </DialogTrigger>
      <DialogContent className='max-h-[calc(100vh-2rem)] max-w-5xl overflow-y-auto'>
        <DialogHeader>
          <DialogTitle>Relations for {getEntityName(entityId)}</DialogTitle>
        </DialogHeader>

        <div className='space-y-6'>
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Primary Field</TableHead>
                <TableHead>Foreign Field</TableHead>
                <TableHead>Relation Type</TableHead>
                <TableHead>Delete Behavior</TableHead>
                <TableHead>Virtual Field</TableHead>
                <TableHead>Virtual Property</TableHead>
                <TableHead className='text-right'>Actions</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {relations.length > 0 ? (
                relations.map(relation => (
                  <TableRow key={relation.id}>
                    <TableCell className='font-medium'>
                      {getFieldEntityName(relation.primaryFieldId) ? (
                        <span className='text-muted-foreground'>
                          {getFieldEntityName(relation.primaryFieldId)}.
                          <span className='text-foreground font-semibold'>{relation.primaryFieldName}</span>
                        </span>
                      ) : (
                        relation.primaryFieldName ?? '-'
                      )}
                    </TableCell>
                    <TableCell className='font-medium'>
                      {getFieldEntityName(relation.foreignFieldId) ? (
                        <span className='text-muted-foreground'>
                          {getFieldEntityName(relation.foreignFieldId)}.
                          <span className='text-foreground font-semibold'>{relation.foreignFieldName}</span>
                        </span>
                      ) : (
                        relation.foreignFieldName ?? '-'
                      )}
                    </TableCell>
                    <TableCell>
                      <span className='inline-flex items-center rounded-md bg-sky-400/10 px-2 py-1 text-xs font-medium text-sky-400 ring-1 ring-inset ring-sky-400/20'>
                        {relation.relationTypeName ?? '-'}
                      </span>
                    </TableCell>
                    <TableCell>
                      <span className='inline-flex items-center rounded-md bg-amber-400/10 px-2 py-1 text-xs font-medium text-amber-400 ring-1 ring-inset ring-amber-400/20'>
                        {relation.deleteBehaviorTypeName ?? '-'}
                      </span>
                    </TableCell>
                    <TableCell>{relation.primaryEntityVirPropName ?? '-'}</TableCell>
                    <TableCell>{relation.foreignEntityVirPropName ?? '-'}</TableCell>
                    <TableCell className='text-right'>
                      <div className='flex justify-end gap-2'>
                        <Button type='button' variant='outline' size='icon-sm' onClick={() => handleEdit(relation)}>
                          <PencilIcon className='size-4' />
                        </Button>
                        <Button type='button' variant='destructive' size='icon-sm' onClick={() => setRelationToDelete(relation.id)}>
                          <TrashIcon className='size-4' />
                        </Button>
                      </div>
                    </TableCell>
                  </TableRow>
                ))
              ) : (
                <TableRow>
                  <TableCell colSpan={7} className='py-6 text-center text-muted-foreground'>
                    No relations found.
                  </TableCell>
                </TableRow>
              )}
            </TableBody>
          </Table>

          <Separator />

          <form id={`form-relation-${entityId}`} onSubmit={form.handleSubmit(onSubmit)} className='space-y-6'>
            <div className='flex items-center justify-between'>
              <h3 className='font-semibold text-lg'>{formMode === 'update' ? 'Edit Relation' : 'Create Relation'}</h3>
              {formMode === 'update' && (
                <Button type='button' variant='outline' size='sm' onClick={resetCreateForm}>
                  <XIcon className='mr-2 size-4' /> Cancel Edit
                </Button>
              )}
            </div>

            <FieldGroup className='grid grid-cols-1 gap-6 md:grid-cols-2'>
              {/* Primary Key side */}
              <div className='space-y-4 rounded-lg border border-border p-4 bg-card shadow-xs'>
                <h4 className='border-b border-border pb-2 font-bold text-sky-500 flex items-center gap-2'>
                  <GitBranch className='size-4' /> Primary Key
                </h4>

                <FormCombobox
                  name='primaryEntityId'
                  control={form.control}
                  label='Entity'
                  items={entities}
                  placeholder='Select primary entity'
                  onValueChange={selectedId => {
                    if (selectedId) handlePrimaryEntityChange(selectedId);
                  }}
                />

                <FormCombobox
                  name='primaryFieldId'
                  control={form.control}
                  label='Field'
                  items={primaryFields}
                  disabled={primaryEntityId === 0}
                  placeholder='Select primary field'
                />

                <FormInput name='primaryEntityVirPropName' control={form.control} label='Virtual Field Name' placeholder='e.g., VirtualFieldName' autoComplete='off' />
              </div>

              {/* Foreign Key side */}
              <div className='space-y-4 rounded-lg border border-border p-4 bg-card shadow-xs'>
                <h4 className='border-b border-border pb-2 font-bold text-sky-500 flex items-center gap-2'>
                  <GitBranch className='rotate-180 size-4' /> Foreign Key
                </h4>

                <FormCombobox
                  name='foreignEntityId'
                  control={form.control}
                  label='Entity'
                  items={entities}
                  placeholder='Select foreign entity'
                  onValueChange={selectedId => {
                    if (selectedId) handleForeignEntityChange(selectedId);
                  }}
                />

                <FormCombobox
                  name='foreignFieldId'
                  control={form.control}
                  label='Field'
                  items={foreignFields}
                  disabled={foreignEntityId === 0}
                  placeholder='Select foreign field'
                />

                <FormInput name='foreignEntityVirPropName' control={form.control} label='Virtual Property Name' placeholder='e.g., VirtualPropName' autoComplete='off' />
              </div>
            </FieldGroup>

            <FieldGroup className='grid grid-cols-1 gap-6 md:grid-cols-2 rounded-lg border border-border p-4 bg-muted/40 shadow-xs'>
              <FormCombobox name='relationTypeId' control={form.control} label='Relation Type' items={relationTypes} placeholder='Select relation type' />
              <FormCombobox name='deleteBehaviorTypeId' control={form.control} label='Delete Behavior Type' items={deleteBehaviorTypes} placeholder='Select delete behavior type' />
            </FieldGroup>
          </form>
        </div>

        <DialogFooter>
          <DialogClose asChild>
            <Button type='button' variant='outline'>
              Close
            </Button>
          </DialogClose>
          <Button type='submit' form={`form-relation-${entityId}`} className='bg-emerald-600 hover:bg-emerald-700 text-white'>
            {formMode === 'update' ? <SaveIcon className='mr-2 size-4' /> : <PlusCircleIcon className='mr-2 size-4' />}
            {formMode === 'update' ? 'Save Relation' : 'Add Relation'}
          </Button>
        </DialogFooter>
        <ConfirmDialog
          open={relationToDelete !== null}
          onOpenChange={open => { if (!open) setRelationToDelete(null); }}
          title='Delete Relation?'
          description='Are you sure you want to delete this relation? This action cannot be undone.'
          confirmLabel='Delete'
          onConfirm={() => {
            if (relationToDelete !== null) {
              handleDelete(relationToDelete);
              setRelationToDelete(null);
            }
          }}
        />
      </DialogContent>
    </Dialog>
  );
}
