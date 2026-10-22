CREATE TABLE audit.catalog_events(
 id uuid NOT NULL,organization_id uuid NOT NULL REFERENCES identity.organizations(id),actor_id uuid NOT NULL REFERENCES identity.subjects(id),
 operation text NOT NULL CHECK(operation IN ('created','updated','archived')),
 resource_kind text NOT NULL CHECK(resource_kind IN ('entity','relationship')),resource_id uuid NOT NULL,
 previous_revision integer,new_revision integer NOT NULL,occurred_at timestamptz NOT NULL DEFAULT now(),
 correlation_id text NOT NULL,changed_fields text[] NOT NULL,PRIMARY KEY(organization_id,id));
CREATE INDEX audit_resource ON audit.catalog_events(organization_id,resource_kind,resource_id,occurred_at,id);
ALTER TABLE audit.catalog_events ENABLE ROW LEVEL SECURITY;
ALTER TABLE audit.catalog_events FORCE ROW LEVEL SECURITY;
CREATE POLICY audit_read ON audit.catalog_events FOR SELECT USING(
 organization_id=nullif(current_setting('app.organization_id',true),'')::uuid
 AND EXISTS(SELECT FROM identity.memberships m WHERE m.organization_id=audit.catalog_events.organization_id AND m.role='organization_admin'));
CREATE POLICY audit_insert ON audit.catalog_events FOR INSERT WITH CHECK(
 organization_id=nullif(current_setting('app.organization_id',true),'')::uuid
 AND EXISTS(SELECT FROM identity.memberships m WHERE m.organization_id=audit.catalog_events.organization_id AND m.role IN ('organization_admin','architect')));
GRANT SELECT,INSERT ON audit.catalog_events TO aip_i1_app;
REVOKE ALL ON public.schema_migrations FROM PUBLIC,aip_i1_app;
