CREATE TABLE catalog.relationships(
 id uuid NOT NULL,organization_id uuid NOT NULL REFERENCES identity.organizations(id),
 type_id text NOT NULL CHECK(type_id IN ('core.supports','core.uses')),
 schema_version text NOT NULL CHECK(schema_version='0.1.0'),source_entity_id uuid NOT NULL,target_entity_id uuid NOT NULL,
 description text CHECK(length(description)<=4000),status text NOT NULL DEFAULT 'active' CHECK(status IN ('active','archived')),
 revision integer NOT NULL DEFAULT 1 CHECK(revision>0),created_at timestamptz NOT NULL DEFAULT now(),updated_at timestamptz NOT NULL DEFAULT now(),
 created_by uuid NOT NULL REFERENCES identity.subjects(id),updated_by uuid NOT NULL REFERENCES identity.subjects(id),
 PRIMARY KEY(organization_id,id),CHECK(source_entity_id<>target_entity_id),
 FOREIGN KEY(type_id,schema_version) REFERENCES catalog.type_definitions(type_id,schema_version),
 FOREIGN KEY(organization_id,source_entity_id) REFERENCES catalog.entities(organization_id,id),
 FOREIGN KEY(organization_id,target_entity_id) REFERENCES catalog.entities(organization_id,id));
CREATE UNIQUE INDEX unique_active_relationship ON catalog.relationships(organization_id,type_id,source_entity_id,target_entity_id) WHERE status='active';
CREATE INDEX relationship_source ON catalog.relationships(organization_id,source_entity_id,status);
CREATE INDEX relationship_target ON catalog.relationships(organization_id,target_entity_id,status);
ALTER TABLE catalog.relationships ENABLE ROW LEVEL SECURITY;
ALTER TABLE catalog.relationships FORCE ROW LEVEL SECURITY;
CREATE POLICY relationship_scope ON catalog.relationships USING(
 organization_id=nullif(current_setting('app.organization_id',true),'')::uuid
 AND EXISTS(SELECT FROM identity.memberships m WHERE m.organization_id=catalog.relationships.organization_id))
 WITH CHECK(organization_id=nullif(current_setting('app.organization_id',true),'')::uuid
 AND EXISTS(SELECT FROM identity.memberships m WHERE m.organization_id=catalog.relationships.organization_id AND m.role IN ('organization_admin','architect')));
GRANT SELECT,INSERT,UPDATE ON catalog.relationships TO aip_i1_app;
