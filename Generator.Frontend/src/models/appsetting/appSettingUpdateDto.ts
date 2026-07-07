import z from 'zod';

export default interface AppSettingUpdateDto {
  id: number;
  projectName: string | null;
  solutionName: string | null;
  path: string | null;
  dBConnectionString: string | null;
  isThereIdentity: boolean;
  isThereUser: boolean;
  userEntityId: number | null;
  isThereRole: boolean;
  roleEntityId: number | null;
}

export const AppSettingUpdateSchema = z.object({
  id: z.number().positive(),
  projectName: z.string().nullable(),
  solutionName: z.string().nullable(),
  path: z.string().nullable(),
  dBConnectionString: z.string().nullable(),
  isThereIdentity: z.boolean(),
  isThereUser: z.boolean(),
  userEntityId: z.number().positive().nullable(),
  isThereRole: z.boolean(),
  roleEntityId: z.number().positive().nullable()
});
