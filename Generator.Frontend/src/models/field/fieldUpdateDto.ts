import z from 'zod';

export default interface FieldUpdateDto {
  id: number;
  fieldTypeId: number;
  name: string;
  isRequired: boolean;
  isUnique: boolean;
  isList: boolean;
  filterable: boolean;
}

export const FieldsUpdateSchema = z.object({
  fields: z.array(
    z.object({
      id: z.number(),
      fieldTypeId: z.number(),
      name: z.string().min(2, 'Must be at least 2 characters'),
      isRequired: z.boolean(),
      isUnique: z.boolean(),
      isList: z.boolean(),
      filterable: z.boolean()
    })
  )
});
