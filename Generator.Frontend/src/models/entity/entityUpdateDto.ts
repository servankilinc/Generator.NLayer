import z from 'zod';

export default interface EntityUpdateDto {
  id: number;
  name: string;
  tableName: string;
  auditable: boolean;
  archivable: boolean;
  softDeletable: boolean;
  createDtoId: number | null;
  updateDtoId: number | null;
  deleteDtoId: number | null;
  reportDtoId: number | null;
  basicResponseDtoId: number | null;
  detailResponseDtoId: number | null;
}

export const EntityUpdateSchema = z.object({
  id: z.number(),
  name: z.string().min(2, 'Must be at least 2 characters'),
  tableName: z.string().min(2, 'Must be at least 2 characters'),
  auditable: z.boolean(),
  archivable: z.boolean(),
  softDeletable: z.boolean(),
  createDtoId: z.number().nullable(),
  updateDtoId: z.number().nullable(),
  deleteDtoId: z.number().nullable(),
  reportDtoId: z.number().nullable(),
  basicResponseDtoId: z.number().nullable(),
  detailResponseDtoId: z.number().nullable()
});
