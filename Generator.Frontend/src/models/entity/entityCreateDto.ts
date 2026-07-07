import type FieldCreateDto from '@/models/field/fieldCreateDto';
import { FieldCreateSchema } from '@/models/field/fieldCreateDto';
import z from 'zod';

export default interface EntityCreateDto {
  name: string;
  tableName: string;
  softDeletable: boolean;
  auditable: boolean;
  archivable: boolean;
  fields: FieldCreateDto[];
}

export const EntityCreateSchema = z.object({
  name: z.string().min(2, 'Entity name must be at least 2 characters'),
  tableName: z.string().min(2, 'Table name must be at least 2 characters'),
  softDeletable: z.boolean(),
  auditable: z.boolean(),
  archivable: z.boolean(),
  fields: z.array(FieldCreateSchema).min(1, 'At least one field is required')
});
