export default interface AppSetting {
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
  control: boolean;
}
