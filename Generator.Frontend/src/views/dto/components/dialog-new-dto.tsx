import { useEffect, useState, type ReactNode, type ChangeEvent } from 'react';
import { z } from 'zod';
import { toast } from 'sonner';
import axiosHelper from '@/lib/axios-helper';
import { fetchEntities } from '@/redux/reducers/entitySlice';
import { useAppDispatch, useAppSelector } from '@/hooks';
import { useFieldArray, useForm, useWatch } from 'react-hook-form';
import { PlusCircleIcon, TrashIcon } from 'lucide-react';
import { zodResolver } from '@hookform/resolvers/zod';
import type CrudType from '@/models/crudType/crudType';
import { DtoCreateSchema } from '@/models/dto/dtoCreateDto';
import type RelationVisualModel from '@/models/dtoFieldRelations/relationVisualModel';
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

type FormData = z.infer<typeof DtoCreateSchema>;
type DtoFieldRowState = {
  relationOptions: RelationVisualModel[];
};

export default function DialogNewDto(props: { entityId: number; onCreated?: () => void; trigger?: ReactNode }) {
  const dispatch = useAppDispatch();

  const [isOpen, setIsOpen] = useState(false);

  const entities = useAppSelector(state => state.entity.entities);
  const [crudTypes, setCrudTypes] = useState<CrudType[]>([]);
  const [rowStates, setRowStates] = useState<Record<string, DtoFieldRowState>>({});

  const form = useForm<FormData>({
    resolver: zodResolver(DtoCreateSchema),
    defaultValues: {
      name: '',
      relatedEntityId: props.entityId,
      crudTypeId: 0,
      dtoFields: []
    }
  });

  const dtoFields = useWatch({
    control: form.control,
    name: 'dtoFields'
  });

  const { fields, append, remove } = useFieldArray({
    control: form.control,
    name: 'dtoFields'
  });

  const getEntityFields = (sourceEntityId: number) => entities.find(entity => entity.id === sourceEntityId)?.fields ?? [];

  async function fetchCrudTypes() {
    try {
      const response = await axiosHelper.get<CrudType[]>('/crudType/list');
      setCrudTypes(response ?? []);
    } catch {
      toast.error('Crud Types Could not Readed!');
    }
  }

  async function syncDtoFieldRelations(rowId: string, index: number, sourceEntityId: number, dtoRelatedEntityId: number) {
    setRowStates(current => ({
      ...current,
      [rowId]: {
        relationOptions: []
      }
    }));
    form.setValue(`dtoFields.${index}.dtoFieldRelations`, null, { shouldValidate: true });

    if (sourceEntityId === 0 || dtoRelatedEntityId === 0 || sourceEntityId === dtoRelatedEntityId) {
      return;
    }

    try {
      const response = await axiosHelper.get<RelationVisualModel[]>('/relation/list/behindEntities', {
        params: {
          firstEntityId: dtoRelatedEntityId,
          secondEntityId: sourceEntityId
        }
      });
      const relationOptions = response ?? [];
      setRowStates(current => ({
        ...current,
        [rowId]: {
          relationOptions
        }
      }));
      form.setValue(
        `dtoFields.${index}.dtoFieldRelations`,
        [
          {
            relationId: relationOptions.length === 1 ? relationOptions[0].id : 0,
            sequenceNo: 1,
            firstEntityId: dtoRelatedEntityId,
            secondEntityId: sourceEntityId
          }
        ],
        { shouldValidate: true }
      );
    } catch {
      toast.error('Relation Visual Models Could not Readed!');
    }
  }

  useEffect(() => {
    // eslint-disable-next-line react-hooks/set-state-in-effect
    fetchCrudTypes();
  }, []);

  useEffect(() => {
    if (isOpen && entities.length === 0) {
      dispatch(fetchEntities());
    }
  }, [dispatch, entities.length, isOpen]);

  useEffect(() => {
    fields.forEach((field, index) => {
      const sourceEntityId = dtoFields?.[index]?.sourceEntityId ?? 0;
      if (sourceEntityId !== 0) {
        syncDtoFieldRelations(field.id, index, sourceEntityId, props.entityId);
      }
    });
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [props.entityId]);

  async function onSubmit(data: FormData) {
    try {
      await axiosHelper.post('/dto', { ...data, relatedEntityId: props.entityId });
      toast.success('DTO Created Successfuly');
      setIsOpen(false);
      form.reset();
      setRowStates({});
      props.onCreated?.();
    } catch {
      toast.error('DTO Could not Bee Created!');
    }
  }

  return (
    <Dialog open={isOpen} onOpenChange={setIsOpen} modal={false}>
      <DialogTrigger asChild>
        {props.trigger ?? (
          <Button variant='destructive' className='w-min m-3'>
            <PlusCircleIcon className='mx-2' /> New DTO
          </Button>
        )}
      </DialogTrigger>
      <DialogContent className='max-w-4xl'>
        <DialogHeader>
          <DialogTitle>Create New DTO</DialogTitle>
          <DialogDescription>Create DTO and define source fields.</DialogDescription>
        </DialogHeader>
        <form id='form-create-dto' onSubmit={form.handleSubmit(onSubmit)} className='space-y-6'>
          <FieldGroup>
            <FormInput name='name' control={form.control} label='Name' id='txt-dto-name' placeholder='name' autoComplete='off' />
            <FormCombobox name='crudTypeId' control={form.control} label='CRUD Type' id='cmbx-crud-type' items={crudTypes} placeholder='Select a CRUD type' />
          </FieldGroup>

          <Separator />

          <div className='space-y-4'>
            <div className='flex items-center justify-between'>
              <h3 className='font-semibold'>DTO Fields</h3>
              <Button
                type='button'
                variant='outline'
                onClick={() =>
                  append({
                    dtoId: 0,
                    sourceEntityId: 0,
                    sourceFieldId: 0,
                    name: '',
                    isRequired: false,
                    isList: false,
                    dtoFieldRelations: null
                  })
                }>
                <PlusCircleIcon className='mr-3' />
                Add Field
              </Button>
            </div>

            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Source Entity</TableHead>
                  <TableHead>Source Field</TableHead>
                  <TableHead className='w-[180px]'>Name</TableHead>
                  <TableHead>Required</TableHead>
                  <TableHead>List</TableHead>
                  <TableHead>Relation</TableHead>
                  <TableHead>Rel. Sequence</TableHead>
                  <TableHead className='text-right'>Actions</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {fields.map((item, index) => (
                  <TableRow key={item.id}>
                    {(() => {
                      const sourceEntityId = dtoFields?.[index]?.sourceEntityId ?? 0;
                      const sourceEntityFields = getEntityFields(sourceEntityId);
                      const relationOptions = rowStates[item.id]?.relationOptions ?? [];
                      const hasRelation = sourceEntityId !== 0 && props.entityId !== 0 && sourceEntityId !== props.entityId;

                      return (
                        <>
                          <TableCell className='font-medium'>
                            <FormCombobox
                              name={`dtoFields.${index}.sourceEntityId`}
                              control={form.control}
                              items={entities}
                              placeholder='Select an entity'
                              onValueChange={selectedEntityId => {
                                form.setValue(`dtoFields.${index}.sourceFieldId`, 0, { shouldValidate: true });
                                form.setValue(`dtoFields.${index}.name`, '', { shouldValidate: true });
                                form.setValue(`dtoFields.${index}.isRequired`, false);
                                form.setValue(`dtoFields.${index}.isList`, false);
                                syncDtoFieldRelations(item.id, index, selectedEntityId ?? 0, props.entityId);
                              }}
                            />
                          </TableCell>
                          <TableCell className='font-medium'>
                            <FormCombobox
                              name={`dtoFields.${index}.sourceFieldId`}
                              control={form.control}
                              items={sourceEntityFields}
                              disabled={sourceEntityId === 0}
                              placeholder='Select a source field'
                              onValueChange={selectedFieldId => {
                                const selected = sourceEntityFields.find(x => x.id === selectedFieldId);
                                if (selected) {
                                  form.setValue(`dtoFields.${index}.name`, selected.name, { shouldValidate: true });
                                  form.setValue(`dtoFields.${index}.isRequired`, selected.isRequired);
                                  form.setValue(`dtoFields.${index}.isList`, selected.isList);
                                }
                              }}
                            />
                          </TableCell>

                          <TableCell className='font-medium'>
                            <FormInput name={`dtoFields.${index}.name`} control={form.control} placeholder='dto field name' autoComplete='off' />
                          </TableCell>

                          <TableCell>
                            <FormCheckbox name={`dtoFields.${index}.isRequired`} control={form.control} className='justify-center' />
                          </TableCell>

                          <TableCell className='font-medium'>
                            <FormCheckbox name={`dtoFields.${index}.isList`} control={form.control} className='justify-center' />
                          </TableCell>
                          
                          <TableCell className='font-medium'>
                            {hasRelation ? (
                              <FormCombobox
                                name={`dtoFields.${index}.dtoFieldRelations.0.relationId`}
                                control={form.control}
                                items={relationOptions}
                                placeholder='Select relation'
                              />
                            ) : null}
                          </TableCell>
                          <TableCell className='font-medium'>
                            {hasRelation ? (
                              <FormInput
                                name={`dtoFields.${index}.dtoFieldRelations.0.sequenceNo`}
                                control={form.control}
                                type='number'
                                min={1}
                                onChange={(event: ChangeEvent<HTMLInputElement>) => form.setValue(`dtoFields.${index}.dtoFieldRelations.0.sequenceNo`, Number(event.target.value))}
                              />
                            ) : null}
                          </TableCell>
                          <TableCell className='text-right'>
                            <Button
                              type='button'
                              variant='destructive'
                              onClick={() => {
                                remove(index);
                                setRowStates(current => {
                                  const next = { ...current };
                                  delete next[item.id];
                                  return next;
                                });
                              }}>
                              <TrashIcon color='red' />
                            </Button>
                          </TableCell>
                        </>
                      );
                    })()}
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
          <Button type='submit' form='form-create-dto'>
            Save
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
