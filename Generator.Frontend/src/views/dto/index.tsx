import { useCallback, useEffect, useState } from 'react';
import BaseSkeleton from '@/components/global/base-skeleton';
import DtoCard from './components/card-dto';
import DialogNewDto from './components/dialog-new-dto';
import type DtoDetailResponseDto from '@/models/dto/dtoDetailResponseDto';
import axiosHelper from '@/lib/axios-helper';
import { toast } from 'sonner';
import { useParams } from 'react-router';

export default function Index() {
  const [dtos, setDtos] = useState<DtoDetailResponseDto[] | null>(null);
  const { entityId } = useParams();

  const fetchDtos = useCallback(async () => {
    try {
      const response = await axiosHelper.get<DtoDetailResponseDto[]>('/dto/list/detail', { params: { entityId: entityId } });
      if (response == null) {
        toast.error('Dtos Could not Readed!');
      } else {
        setDtos(response);
      }
    } catch {
      toast.error('Entities Could not Readed!');
    }
  }, [entityId]);

  useEffect(() => {
    // eslint-disable-next-line react-hooks/set-state-in-effect
    fetchDtos();

    const handleAiCompletion = () => {
      fetchDtos();
    };

    window.addEventListener('ai-generation-completed', handleAiCompletion);
    return () => {
      window.removeEventListener('ai-generation-completed', handleAiCompletion);
    };
  }, [fetchDtos]);

  if (dtos == null) {
    return <BaseSkeleton />;
  }

  return (
    <div className='flex flex-1 flex-col gap-4 p-4 pt-0'>
      <DialogNewDto entityId={Number(entityId) || 0} onCreated={fetchDtos} />
      <div className='grid gap-4 md:grid-cols-2'>
        {dtos != null && dtos.length > 0 ? (
          dtos.map(e => <DtoCard key={`dto-card-${e.id}`} dto={e} onUpdated={fetchDtos} />)
        ) : (
          <>
            <div className='aspect-video rounded-xl bg-muted/50' />
            <div className='aspect-video rounded-xl bg-muted/50' />
            <div className='aspect-video rounded-xl bg-muted/50' />
          </>
        )}
      </div>
    </div>
  );
}
