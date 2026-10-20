CREATE TABLE catalog.type_definitions(
 type_id text NOT NULL,schema_version text NOT NULL,category text NOT NULL CHECK(category IN ('entity','relationship')),
 source_type text,target_type text,definition jsonb NOT NULL,
 PRIMARY KEY(type_id,schema_version));
INSERT INTO catalog.type_definitions VALUES
 ('core.business_capability','0.1.0','entity',NULL,NULL,'{"package_id":"core.catalog","package_version":"0.1.0"}'),
 ('core.application','0.1.0','entity',NULL,NULL,'{"package_id":"core.catalog","package_version":"0.1.0"}'),
 ('core.data_asset','0.1.0','entity',NULL,NULL,'{"package_id":"core.catalog","package_version":"0.1.0"}'),
 ('core.supports','0.1.0','relationship','core.application','core.business_capability','{"package_id":"core.catalog","package_version":"0.1.0"}'),
 ('core.uses','0.1.0','relationship','core.application','core.data_asset','{"package_id":"core.catalog","package_version":"0.1.0"}');
GRANT SELECT ON catalog.type_definitions TO aip_i1_app;
