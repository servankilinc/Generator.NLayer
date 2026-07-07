import z from 'zod';

export default interface FieldCreateDto {
  entityId: number;
  fieldTypeId: number;
  name: string;
  isRequired: boolean;
  isUnique: boolean;
  isList: boolean;
  filterable: boolean;
}

export const FieldCreateSchema = z.object({
  entityId: z.number(),
  fieldTypeId: z.number().min(1, 'Field type is required'),
  name: z.string().min(2, 'Name must be at least 2 characters'),
  isRequired: z.boolean(),
  isUnique: z.boolean(),
  isList: z.boolean(),
  filterable: z.boolean()
});
