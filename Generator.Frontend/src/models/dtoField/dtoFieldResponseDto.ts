export default interface DtoFieldResponseDto {
  id: number;
  name: string;
  dtoId: number;
  sourceFieldName: string;
  entityName: string;
  fieldTypeName: string;
  isRequired: boolean;
  isList: boolean;
  isSourceFromForeignEntity: boolean;
  isThereRelations: boolean;
  dtoFieldRelationsPath: string;
}
