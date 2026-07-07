import type Dto from '../dto/dto';
import type Field from '../field/field';

export default interface Entity {
  id: number;
  tableName: string;
  name: string;
  createDtoId: number | null;
  updateDtoId: number | null;
  deleteDtoId: number | null;
  reportDtoId: number | null;
  basicResponseDtoId: number | null;
  detailResponseDtoId: number | null;
  softDeletable: boolean;
  auditable: boolean;
  archivable: boolean;
  
  // Relations
  createDto: Dto | null;
  updateDto: Dto | null;
  deleteDto: Dto | null;
  reportDto: Dto | null;
  basicResponseDto: Dto | null;
  detailResponseDto: Dto | null;
  fields: Field[] | null;
}
