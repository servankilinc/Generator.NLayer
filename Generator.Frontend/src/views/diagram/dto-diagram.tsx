import { useCallback, useEffect, useState } from 'react';
import { useParams, useNavigate } from 'react-router';
import {
  ReactFlow,
  useNodesState,
  useEdgesState,
  addEdge,
  Background,
  Controls,
  MiniMap,
  Panel,
  type Connection,
  type Edge,
  type Node
} from '@xyflow/react';
import '@xyflow/react/dist/style.css';
import { ArrowLeft, PlusCircleIcon } from 'lucide-react';

import { useAppSelector } from '@/hooks';
import axiosHelper from '@/lib/axios-helper';
import type DtoDetailResponseDto from '@/models/dto/dtoDetailResponseDto';
import DtoNode from './components/dto-node';
import BaseSkeleton from '@/components/global/base-skeleton';
import { toast } from 'sonner';
import DialogNewDto from '@/views/dto/components/dialog-new-dto';

const nodeTypes = {
  dtoNode: DtoNode
};

export default function DtoDiagramView() {
  const { entityId } = useParams();
  const navigate = useNavigate();
  const { entities, loading: entitiesLoading } = useAppSelector(state => state.entity);

  const theme = useAppSelector(state => state.theme.activeTheme);

  const [nodes, setNodes, onNodesChange] = useNodesState<Node>([]);
  const [edges, setEdges, onEdgesChange] = useEdgesState<Edge>([]);
  const [isProcessing, setIsProcessing] = useState(true);
  const [refreshKey, setRefreshKey] = useState(0);

  useEffect(() => {
    if (entitiesLoading || !entities || !entityId) return;

    const entity = entities.find(e => e.id === Number(entityId));
    if (!entity) {
      toast.error('Entity not found.');
      navigate('/diagram');
      return;
    }

    const buildDiagram = async () => {
      setIsProcessing(true);
      try {
        const response = await axiosHelper.get<DtoDetailResponseDto[]>('/dto/list/detail', {
          params: { entityId }
        });

        const newNodes: Node[] = [];

        const DTO_START_X = 50;
        const DTO_START_Y = 50;
        const DTO_X_SPACING = 1000;
        const DTO_Y_SPACING = 600;

        // Add DTO Nodes in a grid
        const dtos = response || [];
        dtos.forEach((dto, index) => {
          const col = index % 2;
          const row = Math.floor(index / 2);

          newNodes.push({
            id: `dto-${dto.id}`,
            type: 'dtoNode',
            position: {
              x: DTO_START_X + col * DTO_X_SPACING,
              y: DTO_START_Y + row * DTO_Y_SPACING
            },
            data: {
              dto,
              onUpdated: () => setRefreshKey(prev => prev + 1)
            }
          });
        });

        setNodes(newNodes);
        setEdges([]);
      } catch {
        toast.error('Failed to load DTOs for this diagram.');
      } finally {
        setIsProcessing(false);
      }
    };

    buildDiagram();
  }, [entities, entitiesLoading, entityId, navigate, setNodes, setEdges, refreshKey]);

  useEffect(() => {
    const handleAiCompletion = () => setRefreshKey(prev => prev + 1);
    window.addEventListener('ai-generation-completed', handleAiCompletion);
    return () => window.removeEventListener('ai-generation-completed', handleAiCompletion);
  }, []);

  const onConnect = useCallback(
    (params: Connection) => setEdges(eds => addEdge(params, eds)),
    [setEdges]
  );

  if (entitiesLoading || isProcessing) {
    return <BaseSkeleton />;
  }

  const entityName = entities.find(e => e.id === Number(entityId))?.name ?? 'Entity';

  return (
    <div className='flex flex-1 flex-col gap-4 p-4 pt-0 w-full h-[calc(100vh-64px)]'>
      <div className='flex items-center gap-2'>
        <button
          onClick={() => navigate('/diagram')}
          className='flex items-center gap-1 text-sm text-muted-foreground hover:text-foreground transition-colors'
        >
          <ArrowLeft className='w-4 h-4' /> Back to Entities
        </button>
        <span className='text-sm text-muted-foreground'>—</span>
        <span className='text-sm font-medium'>{entityName} DTOs</span>
      </div>
      <div className='w-full h-full rounded-xl border border-border bg-card overflow-hidden'>
        <ReactFlow
          nodes={nodes}
          edges={edges}
          onNodesChange={onNodesChange}
          onEdgesChange={onEdgesChange}
          onConnect={onConnect}
          nodeTypes={nodeTypes}
          fitView
          minZoom={0.1}
          maxZoom={1.5}
          colorMode={theme}
          className='bg-muted/10'
        >
          <Background gap={16} size={1} />
          <Controls />
          <MiniMap
            nodeColor={() => 'hsl(var(--muted))'}
          />
          <Panel position='top-right'>
            <DialogNewDto
              entityId={Number(entityId) || 0}
              onCreated={() => setRefreshKey(prev => prev + 1)}
              trigger={
                <button className='flex items-center gap-1.5 px-3 py-2 rounded-md bg-primary text-primary-foreground text-sm font-medium hover:bg-primary/90 transition-colors shadow-sm'>
                  <PlusCircleIcon className='w-4 h-4' /> New DTO
                </button>
              }
            />
          </Panel>
        </ReactFlow>
      </div>
    </div>
  );
}
