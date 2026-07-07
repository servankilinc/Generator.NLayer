import { useEffect } from 'react';
import { useAppDispatch, useAppSelector } from '@/hooks';
import { fetchProjects } from '@/redux/reducers/projectSlice';
import DialogNewProject from '@/views/project/components/dialog-new-project';
import BaseSkeleton from '@/components/global/base-skeleton';
import ProjectCard from './components/card-project';

export default function Projects() {
  const dispatch = useAppDispatch();

  const isLoading = useAppSelector(state => state.project.loading);
  const projects = useAppSelector(state => state.project.projects);

  useEffect(() => {
    dispatch(fetchProjects());
  }, [dispatch]);

  if (isLoading) {
    return <BaseSkeleton />;
  }

  return (
    <div className='flex flex-1 flex-col gap-4 p-4 pt-0'>
      <DialogNewProject />
      <div className='grid gap-4 md:grid-cols-3'>
        {projects != null && projects.length > 0 ? (
          projects.map(p => <ProjectCard key={`project-card-${p.id}`} project={p} />)
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
