import z from 'zod';
import type DtoFieldRelationsCreateModel from '../dtoFieldRelations/dtoFieldRelationsCreateModel';
import { DtoFieldRelationsCreateSchema } from '../dtoFieldRelations/dtoFieldRelationsCreateModel';

export default interface DtoFieldUpdateDto {
  id: number;
  sourceEntityId: number;
  sourceFieldId: number;
  name: string;
  isRequired: boolean;
  isList: boolean;
  dtoFieldRelations: DtoFieldRelationsCreateModel[] | null;
}

export const DtoFieldUpdateSchema = z.object({
  id: z.number(),
  sourceEntityId: z.number(),
  sourceFieldId: z.number(),
  name: z.string().min(2, 'DTO Field name must be at least 2 characters'),
  isRequired: z.boolean(),
  isList: z.boolean(),
  dtoFieldRelations: z.array(DtoFieldRelationsCreateSchema).nullish()
});

export const DtoFieldsUpdateSchema = z.object({
  dtoFields: z.array(DtoFieldUpdateSchema)
});

