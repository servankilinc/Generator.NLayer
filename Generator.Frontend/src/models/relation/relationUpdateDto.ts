import z from 'zod';

export default interface RelationUpdateDto {
  id: number;
  primaryEntityId: number;
  primaryFieldId: number;
  foreignEntityId: number;
  foreignFieldId: number;
  relationTypeId: number;
  deleteBehaviorTypeId: number;
  primaryEntityVirPropName: string | null;
  foreignEntityVirPropName: string | null;
}

export const RelationUpdateSchema = z.object({
  id: z.number().min(1, 'ID is required'),
  primaryEntityId: z.number().min(1, 'Primary entity is required'),
  primaryFieldId: z.number().min(1, 'Primary field is required'),
  foreignEntityId: z.number().min(1, 'Foreign entity is required'),
  foreignFieldId: z.number().min(1, 'Foreign field is required'),
  relationTypeId: z.number().min(1, 'Relation type is required'),
  deleteBehaviorTypeId: z.number().min(1, 'Delete behavior type is required'),
  primaryEntityVirPropName: z.string().nullable(),
  foreignEntityVirPropName: z.string().nullable()
});
