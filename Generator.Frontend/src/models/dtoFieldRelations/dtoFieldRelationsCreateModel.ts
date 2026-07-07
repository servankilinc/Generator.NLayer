import z from 'zod';

export default interface DtoFieldRelationsCreateModel {
  relationId: number;
  sequenceNo: number;
  firstEntityId: number;
  secondEntityId: number;
}

export const DtoFieldRelationsCreateSchema = z.object({
  relationId: z.number(),
  sequenceNo: z.number(),
  firstEntityId: z.number(),
  secondEntityId: z.number()
});
