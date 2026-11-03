import { OccupationalGroup } from "./occupational-group.model";


export interface Position {
  id: string;
  name: string;
  occupationalGroup: OccupationalGroup;
  organizationalStructure : Structure;
}


