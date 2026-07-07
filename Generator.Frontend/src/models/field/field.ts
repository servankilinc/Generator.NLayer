import type FieldType from '../fieldType/fieldType';

export default interface Field {
  id: number;
  entityId: number;
  fieldTypeId: number;
  name: string;
  isUnique: boolean;
  isRequired: boolean;
  isList: boolean;
  filterable: boolean;
  fieldType: FieldType | null;
}
