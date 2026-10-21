export type Role = "organization_admin" | "architect" | "reader";
export interface Organization {
  id: string;
  name: string;
  role: Role;
}
export interface Session {
  username: string | null;
  organizations: Organization[];
}
export interface Entity {
  id: string;
  organization_id: string;
  type_id: string;
  schema_version: string;
  code: string;
  name: string;
  description: string | null;
  classification: string;
  status: string;
  revision: number;
}
export interface Relationship {
  id: string;
  type_id: string;
  source_entity_id: string;
  target_entity_id: string;
  description: string | null;
  status: string;
  revision: number;
}
export interface Audit {
  id: string;
  operation: string;
  resource_kind: string;
  resource_id: string;
  previous_revision: number | null;
  new_revision: number;
  occurred_at: string;
  changed_fields: string[];
}
export interface Page<T> {
  items: T[];
  total: number;
  limit: number;
  offset: number;
}
