import type {
  Entity,
  Relationship,
  Audit,
  Page,
  Session,
} from "../domain/catalog-model";
export class CatalogError extends Error {
  constructor(
    public status: number,
    public code: string,
    message: string,
  ) {
    super(message);
  }
}
export interface EntityDraft {
  type_id: string;
  code: string;
  name: string;
  description: string;
  classification: string;
}
export interface RelationshipDraft {
  type_id: string;
  source_entity_id: string;
  target_entity_id: string;
  description: string;
}
export interface EditableEntity {
  name: string;
  description: string | null;
  classification: string;
}
export interface NewEntity extends EditableEntity {
  type_id: string;
  schema_version: string;
  code: string;
}
export interface NewRelationship {
  type_id: string;
  schema_version: string;
  source_entity_id: string;
  target_entity_id: string;
  description: string | null;
}
export type Filters = Record<string, string | number>;
// An outbound port: the UI/application never constructs URLs, cookies, headers or fetch calls.
export interface CatalogGateway {
  session(): Promise<Session>;
  login(): void;
  logout(): void;
  entities(org: string, filters: Filters): Promise<Page<Entity>>;
  entity(org: string, id: string): Promise<Entity>;
  createEntity(org: string, command: NewEntity): Promise<Entity>;
  editEntity(
    org: string,
    id: string,
    revision: number,
    command: EditableEntity,
  ): Promise<Entity>;
  archiveEntity(org: string, id: string, revision: number): Promise<Entity>;
  relationships(org: string, filters: Filters): Promise<Page<Relationship>>;
  createRelationship(
    org: string,
    command: NewRelationship,
  ): Promise<Relationship>;
  archiveRelationship(
    org: string,
    id: string,
    revision: number,
  ): Promise<Relationship>;
  audit(org: string, filters: Filters): Promise<Page<Audit>>;
}
export class CatalogWorkspace {
  constructor(readonly gateway: CatalogGateway) {}
  saveEntity(
    org: string,
    draft: EntityDraft,
    selected: Entity | null,
  ): Promise<Entity> {
    const editable = {
      name: draft.name,
      description: draft.description || null,
      classification: draft.classification,
    };
    return selected
      ? this.gateway.editEntity(org, selected.id, selected.revision, editable)
      : this.gateway.createEntity(org, {
          ...draft,
          ...editable,
          schema_version: "0.1.0",
        });
  }
  createRelationship(
    org: string,
    draft: RelationshipDraft,
  ): Promise<Relationship> {
    return this.gateway.createRelationship(org, {
      ...draft,
      schema_version: "0.1.0",
      description: draft.description || null,
    });
  }
  async options(
    org: string,
    relationType: string,
    sourceQuery: string,
    targetQuery: string,
  ) {
    const source = await this.gateway.entities(org, {
      limit: 100,
      type_id: "core.application",
      q: sourceQuery,
    });
    const target = await this.gateway.entities(org, {
      limit: 100,
      type_id:
        relationType === "core.supports"
          ? "core.business_capability"
          : "core.data_asset",
      q: targetQuery,
    });
    return { source: source.items, target: target.items };
  }
}
