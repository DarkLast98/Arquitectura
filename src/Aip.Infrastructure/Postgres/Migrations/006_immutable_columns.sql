-- Runtime credentials cannot rewrite identity, definitions, codes or endpoints.
REVOKE UPDATE ON catalog.entities,catalog.relationships FROM aip_i1_app;
GRANT UPDATE(name,description,classification,status,revision,updated_at,updated_by) ON catalog.entities TO aip_i1_app;
GRANT UPDATE(status,revision,updated_at,updated_by) ON catalog.relationships TO aip_i1_app;
