"""AC-01–11,14: HTTP real, PostgreSQL real y rol restringido. Solo datos sintéticos."""

import concurrent.futures, http.cookiejar, json, os, subprocess, unittest, urllib.request, urllib.error, uuid, ssl
from pathlib import Path
from jsonschema import Draft202012Validator, FormatChecker, ValidationError

ROOT = Path(__file__).resolve().parents[1]
BASE = os.environ.get("AIP_TEST_URL", "https://localhost:7240")
A = "00000000-0000-4000-8000-00000000000a"
B = "00000000-0000-4000-8000-00000000000b"
ACTOR = "00000000-0000-4000-8000-000000000002"
SCHEMA = json.loads(
    (ROOT / "contracts/schemas/catalog/0.1.0/catalog.schema.json").read_text()
)


class Client:
    def __init__(self, user=None):
        jar = http.cookiejar.CookieJar()
        if user:
            state = json.loads((ROOT / f".local/i1/sessions/{user}.json").read_text())
            for c in state["cookies"]:
                jar.set_cookie(
                    http.cookiejar.Cookie(
                        0,
                        c["name"],
                        c["value"],
                        None,
                        False,
                        (
                            "localhost.local"
                            if c["domain"] == "localhost"
                            else c["domain"]
                        ),
                        False,
                        False,
                        c["path"],
                        True,
                        c["secure"],
                        None,
                        True,
                        None,
                        None,
                        {},
                        False,
                    )
                )
        self.opener = urllib.request.build_opener(
            urllib.request.HTTPCookieProcessor(jar),
            urllib.request.HTTPSHandler(
                context=ssl.create_default_context(
                    cafile=str(ROOT / ".local/i1/tls/ca.crt")
                )
            ),
        )
        self.csrf = ""
        if user:
            status, session, _ = self.call("GET", "/api/session")
            assert (
                status == 200 and session["username"]
            ), "OIDC session fixture is not authenticated"
            self.csrf = session["csrf_token"]

    def call(self, method, path, body=None, etag=None, csrf=True):
        headers = {}
        if body is not None:
            headers["Content-Type"] = "application/json"
        if method != "GET" and csrf:
            headers["X-CSRF-TOKEN"] = self.csrf
        if etag is not None:
            headers["If-Match"] = str(etag) if isinstance(etag, str) else f'"{etag}"'
        data = (
            body
            if isinstance(body, bytes)
            else None if body is None else json.dumps(body).encode()
        )
        if method == "POST" and data is None:
            data = b""
        request = urllib.request.Request(
            BASE + path, data=data, headers=headers, method=method
        )
        try:
            response = self.opener.open(request, timeout=20)
        except urllib.error.HTTPError as e:
            response = e
        raw = response.read()
        return response.status, json.loads(raw) if raw else None, response.headers

    def api(self, method, path="", body=None, etag=None, org=A, **kw):
        return self.call(
            method, f"/api/v1/organizations/{org}/catalog" + path, body, etag, **kw
        )


def db_sql(sql, owner=False):
    pg = (
        Path(
            os.environ.get(
                "AIP_PG_BIN", "/workspace/.aip-tools/pg/usr/lib/postgresql/17/bin"
            )
        )
        / "psql"
    )
    args = [
        str(pg),
        "-X",
        "-v",
        "ON_ERROR_STOP=1",
        "-At",
        "-d",
        "aip_i1",
        "-p",
        "55432",
    ]
    env = os.environ | {
        "LD_LIBRARY_PATH": os.environ.get(
            "AIP_PG_LIB", "/workspace/.aip-tools/pg/usr/lib/x86_64-linux-gnu"
        )
        + ":"
        + os.environ.get("LD_LIBRARY_PATH", "")
    }
    if owner:
        args += ["-h", str(ROOT / ".local/pgsocket"), "-U", "aip_owner"]
    else:
        config = json.loads((ROOT / ".local/i1/secrets.json").read_text())
        env["PGPASSWORD"] = config["db_password"]
        args += ["-h", "127.0.0.1", "-U", "aip_i1_app"]

    return subprocess.run(
        args, input=sql, text=True, capture_output=True, env=env, check=False
    )


class CatalogTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        checker = FormatChecker()
        if "date-time" not in checker.checkers or checker.conforms(
            "invalid-date-time", "date-time"
        ):
            raise RuntimeError(
                "date-time validation is required; install pinned tests/requirements.txt"
            )
        cls.architect = Client("architect")
        cls.admin = Client("admin")
        cls.reader = Client("reader")
        cls.outsider = Client("outsider")

    def assert_schema(self, name, value):
        Draft202012Validator(
            SCHEMA | {"$ref": "#/$defs/" + name}, format_checker=FormatChecker()
        ).validate(value)

    def entity(
        self, type="core.application", org=A, code=None, name="Elemento sintético"
    ):
        body = {
            "type_id": type,
            "schema_version": "0.1.0",
            "code": code or "T-" + uuid.uuid4().hex[:16],
            "name": name,
        }
        status, row, headers = self.architect.api("POST", "/entities", body, org=org)
        self.assertEqual(status, 201, row)
        self.assert_schema("Entity", row)
        self.assertEqual(headers["ETag"], '"1"')
        return row

    def relation(self, source, target, type="core.supports", org=A):
        return self.architect.api(
            "POST",
            "/relationships",
            {
                "type_id": type,
                "schema_version": "0.1.0",
                "source_entity_id": source["id"],
                "target_entity_id": target["id"],
            },
            org=org,
        )

    def test_01_entities_normalization_and_contract(self):
        code = "n-" + uuid.uuid4().hex[:16]
        e = self.entity(code=" " + code + " ", name="  Aplicación ficticia  ")
        self.assertEqual(e["code"], code.upper())
        self.assertEqual(e["name"], "Aplicación ficticia")
        boundary = self.entity(
            code=" " + uuid.uuid4().hex.upper() * 2 + " ", name="  " + "😀" * 200 + "  "
        )
        self.assertEqual(len(boundary["code"]), 64)
        self.assertEqual(len(boundary["name"]), 200)
        s, row, h = self.architect.api("GET", "/entities/" + e["id"])
        self.assertEqual(s, 200)
        self.assertEqual(row, e)
        for extra in [
            {"organization_id": B},
            {"name": " "},
            {"type_id": "arbitrary"},
            {"schema_version": "99"},
            {"classification": None},
            {"created_by": ACTOR},
            {"code": "bad/code"},
        ]:
            body = {
                "type_id": "core.application",
                "schema_version": "0.1.0",
                "code": "INVALID",
                "name": "Prueba",
            } | extra
            s, p, _ = self.architect.api("POST", "/entities", body)
            self.assertEqual(s, 422, p)
            self.assert_schema("Problem", p)

    def test_02_uniqueness_is_scoped(self):
        e = self.entity()
        self.assertEqual(
            self.architect.api(
                "POST",
                "/entities",
                {
                    "type_id": e["type_id"],
                    "schema_version": "0.1.0",
                    "code": e["code"].lower(),
                    "name": "Duplicado",
                },
            )[0],
            409,
        )
        self.entity(org=B, code=e["code"])
        self.entity(type="core.data_asset", code=e["code"])

    def test_03_relationship_rules(self):
        app = self.entity()
        cap = self.entity("core.business_capability")
        data = self.entity("core.data_asset")
        s, r, _ = self.relation(app, cap)
        self.assertEqual(s, 201, r)
        self.assert_schema("Relationship", r)
        self.assertEqual(self.relation(app, cap)[0], 409)
        self.assertEqual(self.relation(cap, app)[0], 422)
        self.assertEqual(self.relation(app, app)[0], 422)
        self.assertEqual(self.relation(app, data, "core.uses")[0], 201)

    def test_04_cross_organization_no_disclosure(self):
        e = self.entity()
        local = self.entity("core.business_capability", org=B)
        for client in [self.architect, self.admin]:
            s, p, _ = client.api("GET", "/entities/" + e["id"], org=B)
            self.assertEqual(s, 404)
            s, page, _ = client.api("GET", "/entities?code=" + e["code"], org=B)
            self.assertEqual(page["total"], 0)
        self.assertEqual(self.relation(e, local, org=B)[0], 404)
        s, page, _ = self.architect.api(
            "GET", "/relationships?entity_id=" + e["id"], org=B
        )
        self.assertEqual(page["total"], 0)
        s, p, _ = self.architect.api(
            "PATCH", "/entities/" + e["id"], {"name": "Ataque"}, etag=1, org=B
        )
        self.assertEqual(s, 404)
        s, events, _ = self.admin.api(
            "GET", "/audit-events?resource_id=" + e["id"], org=B
        )
        self.assertEqual(events["total"], 0)

    def test_05_auth_permissions_csrf(self):
        self.assertEqual(Client().api("GET", "/entities")[0], 401)
        self.assertEqual(self.outsider.api("GET", "/entities", org=A)[0], 403)
        self.assertEqual(self.reader.api("POST", "/entities", {})[0], 403)
        self.assertEqual(self.architect.api("GET", "/audit-events")[0], 403)
        self.assertEqual(
            self.architect.api("POST", "/entities", {}, csrf=False)[0], 400
        )
        s, p, _ = self.admin.api("GET", "/audit-events")
        self.assertEqual(s, 200)
        self.assert_schema("AuditEventPage", p)

    def test_06_etag_concurrency(self):
        e = self.entity()
        path = "/entities/" + e["id"]
        self.assertEqual(self.architect.api("PATCH", path, {"name": "Prueba"})[0], 428)
        for tag in ["*", 'W/"1"', '"1","2"', '" 1"', '"01"']:
            self.assertEqual(
                self.architect.api("PATCH", path, {"name": "Prueba"}, etag=tag)[0], 400
            )
        with concurrent.futures.ThreadPoolExecutor(2) as pool:
            statuses = list(
                pool.map(
                    lambda n: self.architect.api(
                        "PATCH", path, {"name": "Edición " + str(n)}, etag=1
                    )[0],
                    range(2),
                )
            )
        self.assertEqual(sorted(statuses), [200, 412])
        self.assertEqual(self.architect.api("GET", path)[1]["revision"], 2)

    def test_07_archival_and_history(self):
        app = self.entity()
        cap = self.entity("core.business_capability")
        _, r, _ = self.relation(app, cap)
        self.assertEqual(
            self.architect.api("POST", "/entities/" + app["id"] + "/archive", etag=1)[
                0
            ],
            409,
        )
        self.assertEqual(
            self.architect.api(
                "POST", "/relationships/" + r["id"] + "/archive", etag=1
            )[0],
            200,
        )
        self.assertEqual(
            self.architect.api("POST", "/entities/" + app["id"] + "/archive", etag=1)[
                0
            ],
            200,
        )
        self.assertEqual(
            self.architect.api(
                "PATCH", "/entities/" + app["id"], {"name": "No"}, etag=2
            )[0],
            409,
        )
        self.assertEqual(self.relation(app, cap)[0], 409)
        _, page, _ = self.admin.api("GET", "/audit-events?resource_id=" + app["id"])
        self.assertEqual(
            [i["operation"] for i in page["items"]], ["created", "archived"]
        )

    def test_08_archive_create_races(self):
        for _ in range(6):
            app = self.entity()
            cap = self.entity("core.business_capability")
            with concurrent.futures.ThreadPoolExecutor(2) as pool:
                relation = pool.submit(self.relation, app, cap)
                archive = pool.submit(
                    self.architect.api,
                    "POST",
                    "/entities/" + cap["id"] + "/archive",
                    etag=1,
                )
                s1, r, _ = relation.result()
                s2, e, _ = archive.result()
            self.assertIn((s1, s2), [(201, 409), (409, 200)], (s1, r, s2, e))

    def test_09_audit_failure_rolls_back(self):
        code = ("FAIL-" + uuid.uuid4().hex[:12]).upper()
        create = f"""CREATE FUNCTION test_audit_failure() RETURNS trigger LANGUAGE plpgsql AS $$ BEGIN IF EXISTS(SELECT 1 FROM catalog.entities WHERE id=NEW.resource_id AND code='{code}') THEN RAISE EXCEPTION 'test audit rejection'; END IF; RETURN NEW; END $$; CREATE TRIGGER test_audit_failure BEFORE INSERT ON audit.catalog_events FOR EACH ROW EXECUTE FUNCTION test_audit_failure();"""
        self.assertEqual(db_sql(create, owner=True).returncode, 0)
        try:
            s, _, _ = self.architect.api(
                "POST",
                "/entities",
                {
                    "type_id": "core.application",
                    "schema_version": "0.1.0",
                    "code": code,
                    "name": "Fallo controlado",
                },
            )
            self.assertEqual(s, 500)
            self.assertEqual(
                self.architect.api("GET", "/entities?code=" + code)[1]["total"], 0
            )
        finally:
            db_sql(
                "DROP TRIGGER IF EXISTS test_audit_failure ON audit.catalog_events; DROP FUNCTION IF EXISTS test_audit_failure();",
                owner=True,
            )

    def test_10_database_role_rls_and_context_reset(self):
        e = self.entity()
        other = self.entity(org=B)
        sql = f"""SELECT rolsuper OR rolbypassrls FROM pg_roles WHERE rolname=current_user;
  BEGIN; SELECT set_config('app.subject_id','{ACTOR}',true); SELECT set_config('app.organization_id','{A}',true); SELECT count(*) FROM catalog.entities WHERE id='{other['id']}'; COMMIT;
  BEGIN; SELECT set_config('app.subject_id','{ACTOR}',true); SELECT set_config('app.organization_id','{B}',true); SELECT count(*) FROM catalog.entities WHERE id='{e['id']}'; COMMIT;
  SELECT count(*) FROM catalog.entities;"""
        r = db_sql(sql)
        self.assertEqual(r.returncode, 0, r.stderr)
        lines = r.stdout.splitlines()
        self.assertEqual(lines[0], "f")
        self.assertEqual([x for x in lines if x == "0"], ["0", "0", "0"])
        attack = f"BEGIN; SELECT set_config('app.subject_id','{ACTOR}',true); SELECT set_config('app.organization_id','{A}',true); INSERT INTO catalog.entities SELECT gen_random_uuid(),organization_id,type_id,schema_version,code||'-X',name,description,classification,status,revision,created_at,updated_at,created_by,updated_by FROM catalog.entities WHERE id='{e['id']}'; UPDATE catalog.entities SET organization_id='{B}' WHERE id='{e['id']}'; COMMIT;"
        r = db_sql(attack)
        self.assertNotEqual(r.returncode, 0)
        self.assertIn("permission denied", r.stderr)
        self.assertNotEqual(
            db_sql("UPDATE audit.catalog_events SET operation=operation;").returncode, 0
        )
        insert = f"INSERT INTO catalog.entities(id,organization_id,type_id,schema_version,code,name,classification,created_by,updated_by) VALUES(gen_random_uuid(),'{A}','core.application','0.1.0','NO-CONTEXT','No autorizado','internal','{ACTOR}','{ACTOR}');"
        no_context = db_sql(insert)
        self.assertNotEqual(no_context.returncode, 0)
        self.assertIn("row-level security", no_context.stderr)
        reader_context = f"BEGIN; SELECT set_config('app.subject_id','00000000-0000-4000-8000-000000000003',true); SELECT set_config('app.organization_id','{A}',true);"
        reader_write = db_sql(reader_context + insert)
        self.assertNotEqual(reader_write.returncode, 0)
        self.assertIn("row-level security", reader_write.stderr)

    def test_11_paging_filters_and_errors(self):
        for query in [
            "?limit=0",
            "?unknown=1",
            "?type_id=unknown",
            "?entity_id="
            + str(uuid.uuid4())
            + "&source_entity_id="
            + str(uuid.uuid4()),
        ]:
            path = "/relationships" if "entity_id" in query else "/entities"
            self.assertEqual(self.architect.api("GET", path + query)[0], 400)
        s, page, _ = self.architect.api("GET", "/entities?limit=2&status=all")
        self.assertEqual(s, 200)
        self.assertLessEqual(len(page["items"]), 2)
        self.assert_schema("EntityPage", page)
        s, types, _ = self.architect.api("GET", "/types")
        self.assertEqual(s, 200)
        self.assert_schema("Types", types)
        e = self.entity()
        s, row, h = self.architect.api(
            "PATCH", "/entities/" + e["id"], {"description": None}, etag=1
        )
        self.assertEqual(s, 200)
        self.assert_schema("Entity", row)

    def test_12_audit_permission_follows_endpoint_metadata(self):
        for client, expected in [
            (self.reader, 403),
            (self.architect, 403),
            (self.admin, 200),
        ]:
            for path in ["/audit-events", "/audit-events/", "/AUDIT-EVENTS"]:
                with self.subTest(path=path, expected=expected):
                    self.assertEqual(client.api("GET", path)[0], expected)
        context = f"BEGIN; SET LOCAL app.subject_id='{ACTOR}'; SET LOCAL app.organization_id='{A}'; SELECT count(*) FROM audit.catalog_events; ROLLBACK;"
        result = db_sql(context)
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertIn("0", result.stdout.splitlines())
        for method, path in [
            ("POST", "/entities"),
            ("POST", "/relationships"),
            ("PATCH", "/entities/" + str(uuid.uuid4())),
            ("POST", "/entities/" + str(uuid.uuid4()) + "/archive"),
            ("POST", "/relationships/" + str(uuid.uuid4()) + "/archive"),
        ]:
            self.assertEqual(self.reader.api(method, path, {}, etag=1)[0], 403)

    def test_13_unsupported_unicode_is_validation_and_rolls_back(self):
        entity = self.entity()
        for field in ["name", "description"]:
            for value in ["before\x00after", "\ud800", "\udfff"]:
                with self.subTest(field=field, representation=repr(value)):
                    body = {
                        "type_id": "core.application",
                        "schema_version": "0.1.0",
                        "code": "UNICODE-" + uuid.uuid4().hex[:12],
                        "name": "Unicode test",
                    } | {field: value}
                    status, problem, _ = self.architect.api("POST", "/entities", body)
                    self.assertEqual(status, 422, problem)
                    self.assert_schema("Problem", problem)
                    self.assertEqual(
                        self.architect.api("GET", "/entities?code=" + body["code"])[1][
                            "total"
                        ],
                        0,
                    )
                    self.assertEqual(
                        self.architect.api(
                            "PATCH", "/entities/" + entity["id"], {field: value}, etag=1
                        )[0],
                        422,
                    )
                    self.assertEqual(
                        self.architect.api("GET", "/entities/" + entity["id"])[1],
                        entity,
                    )
        for raw in [b'{"name":"a","name":"b"}', b"{", b'{"\\ud800":1}']:
            self.assertEqual(
                self.architect.api("PATCH", "/entities/" + entity["id"], raw, etag=1)[
                    0
                ],
                400,
            )
        self.assertEqual(
            self.architect.api("GET", "/entities?q=before%00after")[0], 400
        )
        with self.assertRaises(ValidationError):
            self.assert_schema("Entity", entity | {"created_at": "not-a-date"})
        cap = self.entity("core.business_capability")
        self.assertEqual(
            self.architect.api(
                "POST",
                "/relationships",
                {
                    "type_id": "core.supports",
                    "schema_version": "0.1.0",
                    "source_entity_id": entity["id"],
                    "target_entity_id": cap["id"],
                    "description": "bad\x00text",
                },
            )[0],
            422,
        )

    def test_14_archived_uniqueness_and_cross_tenant_foreign_key(self):
        entity = self.entity()
        self.assertEqual(
            self.architect.api(
                "POST", "/entities/" + entity["id"] + "/archive", etag=1
            )[0],
            200,
        )
        body = {
            "type_id": entity["type_id"],
            "schema_version": "0.1.0",
            "code": entity["code"],
            "name": "Cannot reuse archived code",
        }
        self.assertEqual(self.architect.api("POST", "/entities", body)[0], 409)
        self.assertEqual(
            self.architect.api("GET", "/entities?code=" + entity["code"])[1]["total"], 0
        )
        self.assertEqual(
            self.architect.api(
                "GET", "/entities?status=archived&code=" + entity["code"]
            )[1]["total"],
            1,
        )
        self.assertEqual(
            self.architect.api(
                "POST", "/entities/" + entity["id"] + "/archive", etag=2
            )[0],
            409,
        )
        for method, path in [
            ("DELETE", "/entities/" + entity["id"]),
            ("POST", "/entities/" + entity["id"] + "/restore"),
        ]:
            self.assertIn(self.architect.api(method, path)[0], [404, 405])
        app = self.entity()
        cap = self.entity("core.business_capability")
        _, first, _ = self.relation(app, cap)
        self.assertEqual(
            self.architect.api(
                "POST", "/relationships/" + first["id"] + "/archive", etag=1
            )[0],
            200,
        )
        status, second, _ = self.relation(app, cap)
        self.assertEqual(status, 201)
        self.assertNotEqual(first["id"], second["id"])
        foreign = self.entity("core.business_capability", org=B)
        attack = f"BEGIN; SET LOCAL app.subject_id='{ACTOR}'; SET LOCAL app.organization_id='{A}'; INSERT INTO catalog.relationships(id,organization_id,type_id,schema_version,source_entity_id,target_entity_id,created_by,updated_by) VALUES(gen_random_uuid(),'{A}','core.supports','0.1.0','{app['id']}','{foreign['id']}','{ACTOR}','{ACTOR}'); COMMIT;"
        result = db_sql(attack)
        self.assertNotEqual(result.returncode, 0)
        self.assertIn("foreign key constraint", result.stderr)

    def test_15_audit_failure_rolls_back_update_and_archive(self):
        entity = self.entity()
        path = "/entities/" + entity["id"]
        trigger = f"CREATE FUNCTION test_mutation_audit_failure() RETURNS trigger LANGUAGE plpgsql AS $$ BEGIN IF NEW.resource_id='{entity['id']}'::uuid THEN RAISE EXCEPTION 'synthetic audit failure'; END IF; RETURN NEW; END $$; CREATE TRIGGER test_mutation_audit_failure BEFORE INSERT ON audit.catalog_events FOR EACH ROW EXECUTE FUNCTION test_mutation_audit_failure();"
        self.assertEqual(db_sql(trigger, owner=True).returncode, 0)
        try:
            self.assertEqual(
                self.architect.api("PATCH", path, {"name": "Must roll back"}, etag=1)[
                    0
                ],
                500,
            )
            self.assertEqual(self.architect.api("GET", path)[1], entity)
            self.assertEqual(
                self.architect.api("POST", path + "/archive", etag=1)[0], 500
            )
            self.assertEqual(self.architect.api("GET", path)[1], entity)
            self.assertEqual(
                self.admin.api("GET", "/audit-events?resource_id=" + entity["id"])[1][
                    "total"
                ],
                1,
            )
        finally:
            cleanup = db_sql(
                "DROP TRIGGER IF EXISTS test_mutation_audit_failure ON audit.catalog_events; DROP FUNCTION IF EXISTS test_mutation_audit_failure();",
                owner=True,
            )
            self.assertEqual(cleanup.returncode, 0, cleanup.stderr)

    def test_16_order_filters_and_paging_contract(self):
        prefix = "PAGE-" + uuid.uuid4().hex[:10]
        for index in reversed(range(6)):
            self.entity(
                code=prefix + str(index), name=prefix + " Element " + str(index)
            )
        first = self.architect.api("GET", "/entities?limit=2&q=" + prefix)[1]
        second = self.architect.api("GET", "/entities?limit=2&offset=2&q=" + prefix)[1]
        self.assertEqual(first["total"], 6)
        self.assertEqual(second["total"], 6)
        self.assertEqual(
            [e["code"] for e in first["items"] + second["items"]],
            [prefix.upper() + str(n) for n in range(4)],
        )
        for query in [
            "?limit=101",
            "?offset=10001",
            "?offset=-1",
            "?limit=2&limit=3",
            "?status=unknown",
            "?type_id=core.supports",
            "?q=" + ("a" * 129),
        ]:
            self.assertEqual(self.architect.api("GET", "/entities" + query)[0], 400)
        for path, schema in [
            ("/entities?status=all", "EntityPage"),
            ("/relationships?status=all", "RelationshipPage"),
        ]:
            status, page, _ = self.architect.api("GET", path)
            self.assertEqual(status, 200)
            self.assert_schema(schema, page)
        status, page, _ = self.admin.api("GET", "/audit-events?limit=2&offset=1")
        self.assertEqual(status, 200)
        self.assert_schema("AuditEventPage", page)


if __name__ == "__main__":
    import sys

    class EvidenceResult(unittest.TextTestResult):
        def __init__(self, *args, **kwargs):
            super().__init__(*args, **kwargs)
            self.cases = []

        def addSuccess(self, test):
            super().addSuccess(test)
            self.cases.append({"id": test.id(), "result": "PASS"})

        def addFailure(self, test, err):
            super().addFailure(test, err)
            self.cases.append({"id": test.id(), "result": "FAIL"})

        def addError(self, test, err):
            super().addError(test, err)
            self.cases.append({"id": test.id(), "result": "ERROR"})

    result = unittest.TextTestRunner(verbosity=2, resultclass=EvidenceResult).run(
        unittest.defaultTestLoader.loadTestsFromTestCase(CatalogTests)
    )
    if target := os.environ.get("AIP_TEST_REPORT"):
        report = Path(target)
        report.parent.mkdir(parents=True, exist_ok=True)
        report.write_text(
            json.dumps(
                {
                    "total": result.testsRun,
                    "failures": len(result.failures),
                    "errors": len(result.errors),
                    "skipped": len(result.skipped),
                    "cases": result.cases,
                },
                indent=2,
            )
            + "\n"
        )
        report.chmod(0o600)
    sys.exit(0 if result.wasSuccessful() else 1)
