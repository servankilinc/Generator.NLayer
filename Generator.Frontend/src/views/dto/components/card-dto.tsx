import { useState } from 'react';
import { BadgeInfo } from '@/components/ui/badge';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import DialogUpdateDto from './dialog-update-dto';
import { Separator } from '@/components/ui/separator';
import TableDtoFields from './table-dtofields';
import { Button } from '@/components/ui/button';
import { TrashIcon } from 'lucide-react';
import axiosHelper from '@/lib/axios-helper';
import { toast } from 'sonner';
import type DtoDetailResponseDto from '@/models/dto/dtoDetailResponseDto';
import ConfirmDialog from '@/components/global/confirm-dialog';

export default function CardDto({ dto, onUpdated }: { dto: DtoDetailResponseDto; onUpdated?: () => void }) {
  const [showDeleteConfirm, setShowDeleteConfirm] = useState(false);
  const deleteDto = async () => {
    try {
      await axiosHelper.delete('/dto', undefined, { params: { id: dto.id } });
      toast.success('DTO Deleted Successfully');
      onUpdated?.();
    } catch {
      toast.error('DTO Could not Be Deleted!');
    }
  };

  return (
    <Card size='sm' className='mx-auto '>
      <CardHeader>
        <CardTitle className='flex justify-between'>
          <div className='flex gap-2'>
            <span className='font-bold text-xl'>{dto.name}</span>
            <BadgeInfo className='self-center' title='CRUD Type'>
              {dto.crudTypeName}
            </BadgeInfo>
          </div>
          <div className='flex gap-2'>
            <Button variant='destructive' size='icon-sm' onClick={() => setShowDeleteConfirm(true)}>
              <TrashIcon className='size-4' />
            </Button>
            <DialogUpdateDto dtoId={dto.id} onUpdated={onUpdated} />
          </div>
        </CardTitle>
      </CardHeader>
      <ConfirmDialog
        open={showDeleteConfirm}
        onOpenChange={setShowDeleteConfirm}
        title={`Delete DTO "${dto.name}"?`}
        description='This will permanently delete this DTO. This action cannot be undone.'
        confirmLabel='Delete'
        onConfirm={deleteDto}
      />
      <CardContent>
        <Separator />
        <TableDtoFields dtoId={dto.id} onUpdated={onUpdated} />
      </CardContent>
    </Card>
  );
}
