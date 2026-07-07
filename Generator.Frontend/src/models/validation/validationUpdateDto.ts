import z from "zod";
import type ValidationParamUpdateDto from "../validationParam/validationParamUpdateDto";
import { ValidationParamUpdateSchema } from "../validationParam/validationParamUpdateDto";

export default interface ValidationUpdateDto {
  validationId: number;
  dtoFieldId: number;
  validatorTypeId: number;
  errorMessage: string | null;
  validationParams: ValidationParamUpdateDto[] | null;
}

export const ValidationUpdateSchema = z.object({
  validationId: z.number(),
  dtoFieldId: z.number(),
  validatorTypeId: z.number(),
  errorMessage: z.string().nullable(),
  validationParams: z.array(ValidationParamUpdateSchema).nullable()
});