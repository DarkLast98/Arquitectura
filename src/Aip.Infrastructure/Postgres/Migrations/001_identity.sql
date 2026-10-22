CREATE SCHEMA identity;
CREATE SCHEMA catalog;
CREATE SCHEMA audit;
REVOKE CREATE ON SCHEMA public FROM PUBLIC;
CREATE TABLE identity.subjects(
 id uuid PRIMARY KEY, issuer text NOT NULL, external_subject text NOT NULL,
 status text NOT NULL DEFAULT 'active' CHECK(status IN ('active','revoked')),
 created_at timestamptz NOT NULL DEFAULT now(), UNIQUE(issuer,external_subject));
CREATE TABLE identity.organizations(id uuid PRIMARY KEY,name text NOT NULL);
CREATE TABLE identity.memberships(
 organization_id uuid NOT NULL REFERENCES identity.organizations(id),
 subject_id uuid NOT NULL REFERENCES identity.subjects(id),
 role text NOT NULL CHECK(role IN ('organization_admin','architect','reader')),
 status text NOT NULL DEFAULT 'active' CHECK(status IN ('active','revoked')),
 PRIMARY KEY(organization_id,subject_id));
ALTER TABLE identity.subjects ENABLE ROW LEVEL SECURITY;
ALTER TABLE identity.subjects FORCE ROW LEVEL SECURITY;
CREATE POLICY subject_read ON identity.subjects FOR SELECT USING(
 id=nullif(current_setting('app.subject_id',true),'')::uuid OR
 (issuer=current_setting('app.issuer',true) AND external_subject=current_setting('app.external_subject',true)));
CREATE POLICY subject_insert ON identity.subjects FOR INSERT WITH CHECK(
 issuer=current_setting('app.issuer',true) AND external_subject=current_setting('app.external_subject',true) AND status='active');
ALTER TABLE identity.memberships ENABLE ROW LEVEL SECURITY;
ALTER TABLE identity.memberships FORCE ROW LEVEL SECURITY;
CREATE POLICY member_read ON identity.memberships FOR SELECT USING(
 subject_id=nullif(current_setting('app.subject_id',true),'')::uuid AND status='active'
 AND EXISTS(SELECT FROM identity.subjects s WHERE s.id=subject_id AND s.status='active'));
ALTER TABLE identity.organizations ENABLE ROW LEVEL SECURITY;
ALTER TABLE identity.organizations FORCE ROW LEVEL SECURITY;
CREATE POLICY organization_read ON identity.organizations FOR SELECT USING(
 EXISTS(SELECT FROM identity.memberships m WHERE m.organization_id=identity.organizations.id));
GRANT USAGE ON SCHEMA identity,catalog,audit TO aip_i1_app;
GRANT SELECT,INSERT ON identity.subjects TO aip_i1_app;
GRANT SELECT ON identity.memberships,identity.organizations TO aip_i1_app;
