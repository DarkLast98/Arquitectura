import { Component, computed, signal } from "@angular/core";
import { FormsModule } from "@angular/forms";

type Role = "organization_admin" | "architect" | "reader";
interface Organization {
  id: string;
  name: string;
  role: Role;
}
interface Session {
  csrf_token: string;
  username: string | null;
  organizations: Organization[];
}
interface Entity {
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
interface Relationship {
  id: string;
  type_id: string;
  source_entity_id: string;
  target_entity_id: string;
  description: string | null;
  status: string;
  revision: number;
}
interface Audit {
  id: string;
  operation: string;
  resource_kind: string;
  resource_id: string;
  previous_revision: number | null;
  new_revision: number;
  occurred_at: string;
  changed_fields: string[];
}
interface Page<T> {
  items: T[];
  total: number;
  limit: number;
  offset: number;
}
class ApiError extends Error {
  constructor(
    public status: number,
    public code: string,
    message: string,
  ) {
    super(message);
  }
}

@Component({
  selector: "aip-root",
  standalone: true,
  imports: [FormsModule],
  templateUrl: "./catalog-app.html",
})
export class App {
  session = signal<Session | null>(null);
  orgId = signal("");
  busy = signal(false);
  error = signal("");
  notice = signal("");
  tab = signal("entities");
  entities = signal<Entity[]>([]);
  relationships = signal<Relationship[]>([]);
  audit = signal<Audit[]>([]);
  relationTotal = signal(0);
  relationOffset = signal(0);
  relationEntityId = "";
  auditTotal = signal(0);
  auditOffset = signal(0);
  total = signal(0);
  offset = signal(0);
  selected = signal<Entity | null>(null);
  conflict = signal<Entity | null>(null);
  applications = signal<Entity[]>([]);
  targets = signal<Entity[]>([]);
  epoch = 0;
  org = computed(() =>
    this.session()?.organizations.find((o) => o.id === this.orgId()),
  );
  canWrite = computed(() => !!this.org() && this.org()!.role !== "reader");
  isAdmin = computed(() => this.org()?.role === "organization_admin");
  query = "";
  status = "active";
  filterType = "";
  entityDraft = this.emptyEntity();
  sourceQuery = "";
  targetQuery = "";
  relationDraft = {
    type_id: "core.supports",
    source_entity_id: "",
    target_entity_id: "",
    description: "",
  };
  entityTypes = [
    { id: "core.business_capability", label: "Capacidad de negocio" },
    { id: "core.application", label: "Aplicación" },
    { id: "core.data_asset", label: "Activo de datos" },
  ];
  constructor() {
    void this.run(async () => {
      await this.loadSession();
    });
  }
  emptyEntity() {
    return {
      type_id: "core.business_capability",
      code: "",
      name: "",
      description: "",
      classification: "internal",
    };
  }
  label(type: string) {
    return this.entityTypes.find((t) => t.id === type)?.label ?? type;
  }
  async request<T>(
    path: string,
    method = "GET",
    body?: object,
    etag?: number,
  ): Promise<T> {
    const headers: Record<string, string> = {};
    if (body) headers["Content-Type"] = "application/json";
    if (method !== "GET")
      headers["X-CSRF-TOKEN"] = this.session()?.csrf_token ?? "";
    if (etag !== undefined) headers["If-Match"] = '"' + etag + '"';
    const response = await fetch(path, {
      method,
      headers,
      credentials: "same-origin",
      body: body ? JSON.stringify(body) : undefined,
    });
    if (!response.ok) {
      const p = await response.json().catch(() => ({
        title: "Petición no disponible",
        code: "HTTP_" + response.status,
      }));
      throw new ApiError(response.status, p.code, p.title);
    }
    return response.status === 204
      ? (undefined as T)
      : ((await response.json()) as T);
  }
  base() {
    return "/api/v1/organizations/" + this.orgId() + "/catalog";
  }
  async run(action: () => Promise<void>) {
    if (this.busy()) return;
    this.busy.set(true);
    this.error.set("");
    this.notice.set("");
    try {
      await action();
    } catch (e) {
      this.error.set(
        e instanceof ApiError
          ? e.message + " (" + e.code + ")"
          : "No se pudo conectar. Intenta de nuevo.",
      );
    } finally {
      this.busy.set(false);
    }
  }
  async loadSession() {
    const s = await this.request<Session>("/api/session");
    this.session.set(s);
    this.orgId.set(s.organizations[0]?.id ?? "");
    if (this.orgId()) await this.loadEntities();
  }
  login() {
    window.location.assign("/auth/login");
  }
  logout() {
    const form = document.createElement("form");
    form.method = "POST";
    form.action = "/auth/logout";
    const input = document.createElement("input");
    input.type = "hidden";
    input.name = "__RequestVerificationToken";
    input.value = this.session()?.csrf_token ?? "";
    form.appendChild(input);
    document.body.appendChild(form);
    form.submit();
  }

  clearContext() {
    this.epoch++;
    this.entities.set([]);
    this.relationships.set([]);
    this.audit.set([]);
    this.relationTotal.set(0);
    this.relationOffset.set(0);
    this.relationEntityId = "";
    this.auditTotal.set(0);
    this.auditOffset.set(0);
    this.applications.set([]);
    this.targets.set([]);
    this.selected.set(null);
    this.conflict.set(null);
    this.total.set(0);
    this.offset.set(0);
    this.entityDraft = this.emptyEntity();
    this.relationDraft = {
      type_id: "core.supports",
      source_entity_id: "",
      target_entity_id: "",
      description: "",
    };
    this.query = "";
    this.filterType = "";
    this.status = "active";
    this.sourceQuery = "";
    this.targetQuery = "";
  }
  changeOrg(id: string) {
    if (this.busy()) return;
    this.clearContext();
    this.orgId.set(id);
    this.tab.set("entities");
    void this.run(() => this.loadEntities());
  }
  async loadEntities() {
    const epoch = this.epoch;
    const q = new URLSearchParams({
      status: this.status,
      limit: "20",
      offset: String(this.offset()),
    });
    if (this.query) q.set("q", this.query);
    if (this.filterType) q.set("type_id", this.filterType);
    const page = await this.request<Page<Entity>>(
      this.base() + "/entities?" + q,
    );
    if (epoch === this.epoch) {
      this.entities.set(page.items);
      this.total.set(page.total);
    }
  }
  search() {
    this.offset.set(0);
    void this.run(() => this.loadEntities());
  }
  page(delta: number) {
    this.offset.update((x) => Math.max(0, x + delta));
    void this.run(() => this.loadEntities());
  }
  choose(entity: Entity) {
    void this.run(async () => {
      const e = await this.request<Entity>(
        this.base() + "/entities/" + entity.id,
      );
      this.edit(e);
      await this.loadRelations(e.id);
    });
  }
  edit(entity: Entity) {
    this.selected.set(entity);
    this.conflict.set(null);
    this.entityDraft = {
      type_id: entity.type_id,
      code: entity.code,
      name: entity.name,
      description: entity.description ?? "",
      classification: entity.classification,
    };
  }
  newEntity() {
    this.selected.set(null);
    this.conflict.set(null);
    this.relationships.set([]);
    this.entityDraft = this.emptyEntity();
  }
  saveEntity() {
    void this.run(async () => {
      const selected = this.selected();
      const body = selected
        ? {
            name: this.entityDraft.name,
            description: this.entityDraft.description || null,
            classification: this.entityDraft.classification,
          }
        : {
            ...this.entityDraft,
            schema_version: "0.1.0",
            description: this.entityDraft.description || null,
          };
      try {
        const e = await this.request<Entity>(
          this.base() + "/entities" + (selected ? "/" + selected.id : ""),
          selected ? "PATCH" : "POST",
          body,
          selected?.revision,
        );
        this.edit(e);
        this.notice.set(selected ? "Cambios guardados." : "Entidad creada.");
        await this.loadEntities();
      } catch (e) {
        if (e instanceof ApiError && e.status === 412 && selected)
          this.conflict.set(
            await this.request<Entity>(
              this.base() + "/entities/" + selected.id,
            ),
          );
        throw e;
      }
    });
  }
  reloadConflict() {
    const c = this.conflict();
    if (c) this.edit(c);
    this.error.set("");
  }
  archiveEntity() {
    const e = this.selected();
    if (
      !e ||
      !confirm(
        "¿Archivar esta entidad? Sus relaciones activas deben archivarse primero.",
      )
    )
      return;
    void this.run(async () => {
      const archived = await this.request<Entity>(
        this.base() + "/entities/" + e.id + "/archive",
        "POST",
        undefined,
        e.revision,
      );
      this.edit(archived);
      await this.loadEntities();
      this.notice.set("Entidad archivada.");
    });
  }
  async loadRelations(entityId?: string, offset = 0) {
    const p = await this.request<Page<Relationship>>(
      this.base() +
        "/relationships?limit=20&offset=" +
        offset +
        (entityId ? "&entity_id=" + entityId : ""),
    );
    this.relationships.set(p.items);
    this.relationTotal.set(p.total);
    this.relationOffset.set(offset);
    this.relationEntityId = entityId ?? "";
  }
  relationshipRange() {
    return this.relationships().length
      ? `${this.relationOffset() + 1}–${this.relationOffset() + this.relationships().length}`
      : "0";
  }
  relationPage(delta: number) {
    void this.run(() =>
      this.loadRelations(
        this.relationEntityId || undefined,
        Math.max(0, this.relationOffset() + delta),
      ),
    );
  }
  async loadAudit(offset = 0) {
    const p = await this.request<Page<Audit>>(
      this.base() + "/audit-events?limit=20&offset=" + offset,
    );
    this.audit.set(p.items);
    this.auditTotal.set(p.total);
    this.auditOffset.set(offset);
  }
  auditPage(delta: number) {
    void this.run(() =>
      this.loadAudit(Math.max(0, this.auditOffset() + delta)),
    );
  }
  showTab(tab: string) {
    if (this.busy()) return;
    this.tab.set(tab);
    void this.run(async () => {
      if (tab === "entities") {
        await this.loadEntities();
        if (this.selected()) await this.loadRelations(this.selected()!.id);
      }
      if (tab === "relationships") {
        await this.loadRelations();
        await this.loadOptions();
      }
      if (tab === "audit") await this.loadAudit();
    });
  }
  async loadOptions() {
    const source = await this.request<Page<Entity>>(
      this.base() +
        "/entities?limit=100&type_id=core.application&q=" +
        encodeURIComponent(this.sourceQuery),
    );
    this.applications.set(source.items);
    const targetType =
      this.relationDraft.type_id === "core.supports"
        ? "core.business_capability"
        : "core.data_asset";
    const target = await this.request<Page<Entity>>(
      this.base() +
        "/entities?limit=100&type_id=" +
        targetType +
        "&q=" +
        encodeURIComponent(this.targetQuery),
    );
    this.targets.set(target.items);
  }
  findOptions() {
    void this.run(() => this.loadOptions());
  }
  relationTypeChanged() {
    this.relationDraft.target_entity_id = "";
    this.targetQuery = "";
    this.findOptions();
  }
  saveRelation() {
    void this.run(async () => {
      await this.request(this.base() + "/relationships", "POST", {
        ...this.relationDraft,
        schema_version: "0.1.0",
        description: this.relationDraft.description || null,
      });
      await this.loadRelations();
      this.notice.set("Relación creada.");
    });
  }
  archiveRelation(r: Relationship) {
    if (!confirm("¿Archivar esta relación?")) return;
    void this.run(async () => {
      await this.request(
        this.base() + "/relationships/" + r.id + "/archive",
        "POST",
        undefined,
        r.revision,
      );
      await this.loadRelations(
        this.tab() === "entities" ? this.selected()?.id : undefined,
      );
      this.notice.set("Relación archivada.");
    });
  }
  endpointName(id: string) {
    return (
      [...this.entities(), ...this.applications(), ...this.targets()].find(
        (e) => e.id === id,
      )?.name ?? id
    );
  }
  navigate(id: string) {
    void this.run(async () => {
      const e = await this.request<Entity>(this.base() + "/entities/" + id);
      this.tab.set("entities");
      this.edit(e);
      await this.loadRelations(e.id);
    });
  }
}
