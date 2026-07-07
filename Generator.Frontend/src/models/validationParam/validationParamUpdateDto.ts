import z from "zod";

export default interface ValidationParamUpdateDto {
  key: string | null;
  validationId: number;
  validatorTypeParamId: number;
  value: string;
}

export const ValidationParamUpdateSchema = z.object({
  validationId: z.number(),
  validatorTypeParamId: z.number(),
  value: z.string()
});