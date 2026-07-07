import type DtoFieldResponseDto from '../dtoField/dtoFieldResponseDto';

export default interface DtoDetailResponseDto {
  id: number;
  name: string;
  relatedEntityName: string;
  crudTypeName: string;
  dtoFields: DtoFieldResponseDto[];
}
