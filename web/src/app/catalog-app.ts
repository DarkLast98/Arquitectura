import { Component, computed, signal, inject } from "@angular/core";
import { FormsModule } from "@angular/forms";
import type {
  Entity,
  Relationship,
  Audit,
  Session,
} from "./domain/catalog-model";
import { CatalogError } from "./application/catalog-workspace";
import { CATALOG } from "./catalog-token";

@Component({
  selector: "aip-root",
  standalone: true,
  imports: [FormsModule],
  templateUrl: "./catalog-app.html",
})
export class App {
  private readonly catalog = inject(CATALOG);
  private readonly gateway = this.catalog.gateway;
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
  async run(action: () => Promise<void>) {
    if (this.busy()) return;
    this.busy.set(true);
    this.error.set("");
    this.notice.set("");
    try {
      await action();
    } catch (e) {
      this.error.set(
        e instanceof CatalogError
          ? e.message + " (" + e.code + ")"
          : "No se pudo conectar. Intenta de nuevo.",
      );
    } finally {
      this.busy.set(false);
    }
  }
  async loadSession() {
    const s = await this.gateway.session();
    this.session.set(s);
    this.orgId.set(s.organizations[0]?.id ?? "");
    if (this.orgId()) await this.loadEntities();
  }
  login() {
    this.gateway.login();
  }
  logout() {
    this.gateway.logout();
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
    const page = await this.gateway.entities(this.orgId(), {
      status: this.status,
      limit: 20,
      offset: this.offset(),
      ...(this.query ? { q: this.query } : {}),
      ...(this.filterType ? { type_id: this.filterType } : {}),
    });
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
      const e = await this.gateway.entity(this.orgId(), entity.id);
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
      try {
        const e = await this.catalog.saveEntity(
          this.orgId(),
          this.entityDraft,
          selected,
        );
        this.edit(e);
        this.notice.set(selected ? "Cambios guardados." : "Entidad creada.");
        await this.loadEntities();
      } catch (e) {
        if (e instanceof CatalogError && e.status === 412 && selected)
          this.conflict.set(
            await this.gateway.entity(this.orgId(), selected.id),
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
      const archived = await this.gateway.archiveEntity(
        this.orgId(),
        e.id,
        e.revision,
      );
      this.edit(archived);
      await this.loadEntities();
      this.notice.set("Entidad archivada.");
    });
  }
  async loadRelations(entityId?: string, offset = 0) {
    const p = await this.gateway.relationships(this.orgId(), {
      limit: 20,
      offset,
      ...(entityId ? { entity_id: entityId } : {}),
    });
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
    const p = await this.gateway.audit(this.orgId(), { limit: 20, offset });
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
    const options = await this.catalog.options(
      this.orgId(),
      this.relationDraft.type_id,
      this.sourceQuery,
      this.targetQuery,
    );
    this.applications.set(options.source);
    this.targets.set(options.target);
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
      await this.catalog.createRelationship(this.orgId(), this.relationDraft);
      await this.loadRelations();
      this.notice.set("Relación creada.");
    });
  }
  archiveRelation(r: Relationship) {
    if (!confirm("¿Archivar esta relación?")) return;
    void this.run(async () => {
      await this.gateway.archiveRelationship(this.orgId(), r.id, r.revision);
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
      const e = await this.gateway.entity(this.orgId(), id);
      this.tab.set("entities");
      this.edit(e);
      await this.loadRelations(e.id);
    });
  }
}
