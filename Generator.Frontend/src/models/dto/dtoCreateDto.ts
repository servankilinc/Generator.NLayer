import z from 'zod';
import type DtoFieldCreateDto from '../dtoField/dtoFieldCreateDto';
import { DtoFieldCreateSchema } from '../dtoField/dtoFieldCreateDto';

export default interface DtoCreateDto {
  name: string;
  relatedEntityId: number;
  crudTypeId: number;
  dtoFields: DtoFieldCreateDto[] | null;
}

export const DtoCreateSchema = z.object({
  name: z.string().min(2, 'Name must be at least 2 characters'),
  relatedEntityId: z.number(),
  crudTypeId: z.number(),
  dtoFields: z.array(DtoFieldCreateSchema).nullish()
});
