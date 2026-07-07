import z from 'zod';

export default interface DtoUpdateDto {
  id: number;
  name: string;
  relatedEntityId: number;
  crudTypeId: number;
}

export const DtoUpdateSchema = z.object({
  id: z.number(),
  name: z.string().min(2, 'Name must be at least 2 characters'),
  relatedEntityId: z.number(),
  crudTypeId: z.number()
});
