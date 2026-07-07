import { useState } from 'react';
import { BadgeDanger, BadgeInfo, BadgeSuccess } from '@/components/ui/badge';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Checkbox } from '@/components/ui/checkbox';
import { Collapsible, CollapsibleContent, CollapsibleTrigger } from '@/components/ui/collapsible';
import { Field, FieldGroup, FieldLabel } from '@/components/ui/field';
import type Entity from '@/models/entity/entity';
import DialogUpdateEntity from './dialog-update-entity';
import { Separator } from '@/components/ui/separator';
import TableFields from './table-fields';
import { ChevronDown, Diamond, TrashIcon } from 'lucide-react';
import { Link } from 'react-router';
import { Button } from '@/components/ui/button';
import DialogRelations from './dialog-relations';
import { useAppDispatch } from '@/hooks';
import { fetchEntities } from '@/redux/reducers/entitySlice';
import axiosHelper from '@/lib/axios-helper';
import { toast } from 'sonner';
import ConfirmDialog from '@/components/global/confirm-dialog';

export default function CardEntity({ entity }: { entity: Entity }) {
  const dispatch = useAppDispatch();
  const [showDeleteConfirm, setShowDeleteConfirm] = useState(false);

  const deleteEntity = async () => {
    try {
      await axiosHelper.delete('/entity', undefined, { params: { id: entity.id } });
      toast.success('Entity Deleted Successfully');
      dispatch(fetchEntities());
    } catch {
      toast.error('Entity Could not Be Deleted!');
    }
  };

  return (
    <Card size='sm' className='mx-auto w-full h-min'>
      <CardHeader>
        <CardTitle className='flex justify-between'>
          <div className='flex gap-2'>
            <span className='font-bold text-xl'>{entity.name}</span>
            <BadgeInfo className='self-center' title='Database Table Name'>
              {entity.tableName}
            </BadgeInfo>
          </div>
          <div className='flex gap-2'>
            <Link to={`/dtos/${entity.id}`}>
              <Button variant='ghost' className='bg-green-600' size='sm'>
                <Diamond className='size-4 mr-2' /> Dtos
              </Button>
            </Link>
            <DialogRelations entityId={entity.id} />
            <Button variant='destructive' size='icon-sm' onClick={() => setShowDeleteConfirm(true)}>
              <TrashIcon className='size-4' />
            </Button>
            <DialogUpdateEntity entityId={entity.id} />
          </div>
        </CardTitle>
      </CardHeader>
      <ConfirmDialog
        open={showDeleteConfirm}
        onOpenChange={setShowDeleteConfirm}
        title={`Delete entity "${entity.name}"?`}
        description='This will permanently delete this entity and all its fields. This action cannot be undone.'
        confirmLabel='Delete'
        onConfirm={deleteEntity}
      />
      <CardContent>
        <div className='flex flex-col gap-2'>
          <Separator />
          <FieldGroup className='grid grid-cols-1 gap-2 p-3 sm:grid-cols-3'>
            <Field orientation='horizontal'>
              <Checkbox checked={entity.auditable} name='terms-checkbox' disabled />
              <FieldLabel>Auditable</FieldLabel>
            </Field>
            <Field orientation='horizontal'>
              <Checkbox checked={entity.archivable} name='terms-checkbox' disabled />
              <FieldLabel>Archivable</FieldLabel>
            </Field>
            <Field orientation='horizontal'>
              <Checkbox checked={entity.softDeletable} name='terms-checkbox' disabled />
              <FieldLabel>Soft Delete</FieldLabel>
            </Field>
          </FieldGroup>
          <Collapsible className='group space-y-2 mb-3'>
            <CollapsibleTrigger className='flex w-full items-center justify-between rounded-md border px-3 py-2 text-sm font-medium hover:bg-muted/50'>
              Models
              <ChevronDown className='size-4 transition-transform group-data-[state=open]:rotate-180' />
            </CollapsibleTrigger>
            <CollapsibleContent>
              <div className='grid grid-cols-1 gap-3 rounded-md border p-3 sm:grid-cols-2'>
                <Field orientation='horizontal'>
                  <span className='font-light'>Create Model</span>
                  <span className='font-medium'>
                    {entity.createDtoId != null ? <BadgeSuccess>{entity.createDto?.name ?? 'OK'}</BadgeSuccess> : <BadgeDanger>NO</BadgeDanger>}
                  </span>
                </Field>
                <Field orientation='horizontal'>
                  <span className='font-light'>Update Model</span>
                  <span className='font-medium'>
                    {entity.updateDtoId != null ? <BadgeSuccess>{entity.updateDto?.name ?? 'OK'}</BadgeSuccess> : <BadgeDanger>NO</BadgeDanger>}
                  </span>
                </Field>
                <Field orientation='horizontal'>
                  <span className='font-light'>Delete Model</span>
                  <span className='font-medium'>
                    {entity.deleteDtoId != null ? <BadgeSuccess>{entity.deleteDto?.name ?? 'OK'}</BadgeSuccess> : <BadgeDanger>NO</BadgeDanger>}
                  </span>
                </Field>
                <Field orientation='horizontal'>
                  <span className='font-light'>Base Model</span>
                  <span className='font-medium'>
                    {entity.basicResponseDtoId != null ? (
                      <BadgeSuccess>{entity.basicResponseDto?.name ?? 'OK'}</BadgeSuccess>
                    ) : (
                      <BadgeDanger>NO</BadgeDanger>
                    )}
                  </span>
                </Field>
                <Field orientation='horizontal'>
                  <span className='font-light'>Detail Model</span>
                  <span className='font-medium'>
                    {entity.detailResponseDtoId != null ? (
                      <BadgeSuccess>{entity.detailResponseDto?.name ?? 'OK'}</BadgeSuccess>
                    ) : (
                      <BadgeDanger>NO</BadgeDanger>
                    )}
                  </span>
                </Field>
                <Field orientation='horizontal'>
                  <span className='font-light'>Report Model</span>
                  <span className='font-medium'>
                    {entity.reportDtoId != null ? <BadgeSuccess>{entity.reportDto?.name ?? 'OK'}</BadgeSuccess> : <BadgeDanger>NO</BadgeDanger>}
                  </span>
                </Field>
              </div>
            </CollapsibleContent>
          </Collapsible>
        </div>
        <TableFields entityId={entity.id} />
      </CardContent>
    </Card>
  );
}
