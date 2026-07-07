import z from 'zod';
import type DtoFieldRelationsCreateModel from '../dtoFieldRelations/dtoFieldRelationsCreateModel';
import { DtoFieldRelationsCreateSchema } from '../dtoFieldRelations/dtoFieldRelationsCreateModel';

export default interface DtoFieldCreateDto {
  dtoId: number;
  sourceEntityId: number;
  sourceFieldId: number;
  name: string;
  isRequired: boolean;
  isList: boolean;
  dtoFieldRelations: DtoFieldRelationsCreateModel[] | null;
}

export const DtoFieldCreateSchema = z.object({
  dtoId: z.number(),
  sourceEntityId: z.number(),
  sourceFieldId: z.number(),
  name: z.string().min(2, 'DTO Field name must be at least 2 characters'),
  isRequired: z.boolean(),
  isList: z.boolean(),
  dtoFieldRelations: z.array(DtoFieldRelationsCreateSchema).nullish()
});
