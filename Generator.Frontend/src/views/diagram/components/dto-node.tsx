import { useState } from 'react';
import { Handle, Position } from '@xyflow/react';
import { TrashIcon } from 'lucide-react';
import { toast } from 'sonner';
import { BadgeInfo } from '@/components/ui/badge';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Separator } from '@/components/ui/separator';
import axiosHelper from '@/lib/axios-helper';
import ConfirmDialog from '@/components/global/confirm-dialog';
import DialogUpdateDto from '@/views/dto/components/dialog-update-dto';
import TableDtoFields from '@/views/dto/components/table-dtofields';
import type DtoDetailResponseDto from '@/models/dto/dtoDetailResponseDto';

interface DtoNodeProps {
  data: {
    dto: DtoDetailResponseDto;
    onUpdated?: () => void;
  };
}

export default function DtoNode({ data }: DtoNodeProps) {
  const { dto, onUpdated } = data;
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
    <div className='w-[850px] rounded-lg border border-border bg-card text-card-foreground shadow-sm nodrag nowheel'>
      {/* Top Handle for incoming connections */}
      <Handle type='target' position={Position.Top} className='w-2.5 h-2.5 !bg-primary' />

      <Card size='sm' className='border-none shadow-none bg-transparent'>
        <CardHeader className='pb-3'>
          <CardTitle className='flex justify-between items-center'>
            <div className='flex items-center gap-2'>
              <span className='font-bold text-lg text-foreground'>{dto.name}</span>
              <BadgeInfo className='self-center' title='CRUD Type'>
                {dto.crudTypeName}
              </BadgeInfo>
            </div>
            <div className='flex gap-1.5 items-center nodrag'>
              <Button
                variant='destructive'
                size='icon-xs'
                onClick={() => setShowDeleteConfirm(true)}
                title='Delete DTO'
              >
                <TrashIcon className='size-3.5' />
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
        <CardContent className='pt-0'>
          <Separator className='mb-4' />
          <div className='overflow-x-auto'>
            <TableDtoFields dtoId={dto.id} onUpdated={onUpdated} />
          </div>
        </CardContent>
      </Card>

      {/* Bottom Handle */}
      <Handle type='source' position={Position.Bottom} className='w-2.5 h-2.5 !bg-primary' />
    </div>
  );
}
