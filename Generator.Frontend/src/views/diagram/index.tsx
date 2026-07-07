import { useCallback, useEffect, useState } from 'react';
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

import { useAppDispatch, useAppSelector } from '@/hooks';
import { fetchEntities } from '@/redux/reducers/entitySlice';
import axiosHelper from '@/lib/axios-helper';
import type RelationDetailModel from '@/models/relation/relationDetailModel';
import EntityNode from './components/entity-node';
import BaseSkeleton from '@/components/global/base-skeleton';
import DialogNewEntity from '@/views/entity/components/dialog-new-entity';
import { toast } from 'sonner';
import { PlusCircleIcon } from 'lucide-react';

const nodeTypes = {
  entityNode: EntityNode
};

export default function DiagramView() {
  const dispatch = useAppDispatch();
  const { entities, loading } = useAppSelector(state => state.entity);
  const theme = useAppSelector(state => state.theme.activeTheme);

  const [nodes, setNodes, onNodesChange] = useNodesState<Node>([]);
  const [edges, setEdges, onEdgesChange] = useEdgesState<Edge>([]);
  const [isProcessing, setIsProcessing] = useState(false);

  // Initial fetch of entities
  useEffect(() => {
    dispatch(fetchEntities());
  }, [dispatch]);

  // Build diagram with entities + relation edges
  useEffect(() => {
    if (loading || !entities || entities.length === 0) return;

    const buildDiagram = async () => {
      setIsProcessing(true);

      // Build entity nodes
      const newNodes: Node[] = [];
      const ENTITY_START_X = 50;
      const ENTITY_START_Y = 50;
      const X_SPACING = 600;
      const Y_SPACING = 500;
      const columns = 3;

      entities.forEach((entity, index) => {
        const col = index % columns;
        const row = Math.floor(index / columns);

        newNodes.push({
          id: `entity-${entity.id}`,
          type: 'entityNode',
          position: {
            x: ENTITY_START_X + col * X_SPACING,
            y: ENTITY_START_Y + row * Y_SPACING
          },
          data: { entity }
        });
      });

      setNodes(newNodes);

      // Fetch relations and build edges
      try {
        const relations = await axiosHelper.get<RelationDetailModel[]>('/relation/list');
        const relationList = relations ?? [];

        const newEdges: Edge[] = [];
        for (const relation of relationList) {
          newEdges.push({
            id: `relation-${relation.id}`,
            source: `entity-${relation.primaryEntityId}`,
            sourceHandle: `field-${relation.primaryFieldId}-right`,
            target: `entity-${relation.foreignEntityId}`,
            targetHandle: `field-${relation.foreignFieldId}-left`,
            label: relation.relationTypeName ?? '',
            animated: true
          });
        }

        setEdges(newEdges);
      } catch {
        toast.error('Failed to load relations.');
        setEdges([]);
      } finally {
        setIsProcessing(false);
      }
    };

    buildDiagram();
  }, [entities, loading, setNodes, setEdges]);

  const onConnect = useCallback(
    (params: Connection) => setEdges(eds => addEdge(params, eds)),
    [setEdges]
  );

  if (loading || isProcessing) {
    return <BaseSkeleton />;
  }

  return (
    <div className='flex flex-1 flex-col gap-4 p-4 pt-0 w-full h-[calc(100vh-64px)]'>
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
            nodeColor={n => {
              if (n.type === 'entityNode') return 'hsl(var(--primary))';
              return 'hsl(var(--muted))';
            }}
          />
          <Panel position='top-right'>
            <DialogNewEntity
              trigger={
                <button className='flex items-center gap-1.5 px-3 py-2 rounded-md bg-primary text-primary-foreground text-sm font-medium hover:bg-primary/90 transition-colors shadow-sm'>
                  <PlusCircleIcon className='w-4 h-4' /> New Entity
                </button>
              }
            />
          </Panel>
        </ReactFlow>
      </div>
    </div>
  );
}
