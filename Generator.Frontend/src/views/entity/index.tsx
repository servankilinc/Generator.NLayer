import { useEffect } from 'react';
import { useAppDispatch, useAppSelector } from '@/hooks';
import { fetchEntities } from '@/redux/reducers/entitySlice';
import BaseSkeleton from '@/components/global/base-skeleton';
import EntityCard from './components/card-entity';
import DialogNewEntity from './components/dialog-new-entity';

export default function Entities() {
  const dispatch = useAppDispatch();
  const isLoading = useAppSelector(state => state.entity.loading);
  const entities = useAppSelector(state => state.entity.entities);

  useEffect(() => {
    dispatch(fetchEntities());
  }, [dispatch]);

  if (isLoading) {
    return <BaseSkeleton />;
  }

  return (
    <div className='flex flex-1 flex-col gap-4 p-4 pt-0'>
      <DialogNewEntity />
      <div className='grid gap-4 md:grid-cols-2'>
        {entities != null && entities.length > 0 ? (
          entities.map(e => <EntityCard key={`entity-card-${e.id}`} entity={e} />)
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
