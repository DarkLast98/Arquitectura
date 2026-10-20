CREATE TABLE catalog.entities(
 id uuid NOT NULL,organization_id uuid NOT NULL REFERENCES identity.organizations(id),
 type_id text NOT NULL CHECK(type_id IN ('core.business_capability','core.application','core.data_asset')),
 schema_version text NOT NULL CHECK(schema_version='0.1.0'),code text NOT NULL,
 name text NOT NULL CHECK(length(trim(name)) BETWEEN 1 AND 200),description text CHECK(length(description)<=4000),
 classification text NOT NULL CHECK(classification IN ('internal','confidential','restricted')),
 status text NOT NULL DEFAULT 'active' CHECK(status IN ('active','archived')),
 revision integer NOT NULL DEFAULT 1 CHECK(revision>0),
 created_at timestamptz NOT NULL DEFAULT now(),updated_at timestamptz NOT NULL DEFAULT now(),
 created_by uuid NOT NULL REFERENCES identity.subjects(id),updated_by uuid NOT NULL REFERENCES identity.subjects(id),
 PRIMARY KEY(organization_id,id),UNIQUE(organization_id,type_id,code),
 FOREIGN KEY(type_id,schema_version) REFERENCES catalog.type_definitions(type_id,schema_version),
 CHECK(code=upper(code) AND code ~ '^[A-Z0-9][A-Z0-9._-]{0,63}$'));
CREATE INDEX entities_order ON catalog.entities(organization_id,code,id);
ALTER TABLE catalog.entities ENABLE ROW LEVEL SECURITY;
ALTER TABLE catalog.entities FORCE ROW LEVEL SECURITY;
CREATE POLICY entity_scope ON catalog.entities USING(
 organization_id=nullif(current_setting('app.organization_id',true),'')::uuid
 AND EXISTS(SELECT FROM identity.memberships m WHERE m.organization_id=catalog.entities.organization_id))
 WITH CHECK(organization_id=nullif(current_setting('app.organization_id',true),'')::uuid
 AND EXISTS(SELECT FROM identity.memberships m WHERE m.organization_id=catalog.entities.organization_id AND m.role IN ('organization_admin','architect')));
GRANT SELECT,INSERT,UPDATE ON catalog.entities TO aip_i1_app;
