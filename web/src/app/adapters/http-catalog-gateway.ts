import type {
  Entity,
  Relationship,
  Audit,
  Page,
  Session,
} from "../domain/catalog-model";
import {
  CatalogError,
  type CatalogGateway,
  type Filters,
  type NewEntity,
  type EditableEntity,
  type NewRelationship,
} from "../application/catalog-workspace";

export class HttpCatalogGateway implements CatalogGateway {
  private csrf = "";
  async session(): Promise<Session> {
    const { csrf_token, ...session } = await this.request<
      Session & { csrf_token: string }
    >("/api/session");
    this.csrf = csrf_token;
    return session;
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
    input.value = this.csrf;
    form.appendChild(input);
    document.body.appendChild(form);
    form.submit();
  }
  entities(org: string, filters: Filters) {
    return this.request<Page<Entity>>(
      this.base(org) + "/entities?" + this.query(filters),
    );
  }
  entity(org: string, id: string) {
    return this.request<Entity>(
      this.base(org) + "/entities/" + encodeURIComponent(id),
    );
  }
  createEntity(org: string, command: NewEntity) {
    return this.request<Entity>(this.base(org) + "/entities", "POST", command);
  }
  editEntity(
    org: string,
    id: string,
    revision: number,
    command: EditableEntity,
  ) {
    return this.request<Entity>(
      this.base(org) + "/entities/" + encodeURIComponent(id),
      "PATCH",
      command,
      revision,
    );
  }
  archiveEntity(org: string, id: string, revision: number) {
    return this.request<Entity>(
      this.base(org) + "/entities/" + encodeURIComponent(id) + "/archive",
      "POST",
      undefined,
      revision,
    );
  }
  relationships(org: string, filters: Filters) {
    return this.request<Page<Relationship>>(
      this.base(org) + "/relationships?" + this.query(filters),
    );
  }
  createRelationship(org: string, command: NewRelationship) {
    return this.request<Relationship>(
      this.base(org) + "/relationships",
      "POST",
      command,
    );
  }
  archiveRelationship(org: string, id: string, revision: number) {
    return this.request<Relationship>(
      this.base(org) + "/relationships/" + encodeURIComponent(id) + "/archive",
      "POST",
      undefined,
      revision,
    );
  }
  audit(org: string, filters: Filters) {
    return this.request<Page<Audit>>(
      this.base(org) + "/audit-events?" + this.query(filters),
    );
  }
  private base(org: string) {
    return "/api/v1/organizations/" + encodeURIComponent(org) + "/catalog";
  }
  private query(filters: Filters) {
    return new URLSearchParams(
      Object.entries(filters).map(([key, value]) => [key, String(value)]),
    );
  }
  private async request<T>(
    path: string,
    method = "GET",
    body?: object,
    etag?: number,
  ): Promise<T> {
    const headers: Record<string, string> = {};
    if (body) headers["Content-Type"] = "application/json";
    if (method !== "GET") headers["X-CSRF-TOKEN"] = this.csrf;
    if (etag !== undefined) headers["If-Match"] = '"' + etag + '"';
    const response = await fetch(path, {
      method,
      headers,
      credentials: "same-origin",
      body: body ? JSON.stringify(body) : undefined,
    });
    if (!response.ok) {
      const p = await response
        .json()
        .catch(() => ({
          title: "Petición no disponible",
          code: "HTTP_" + response.status,
        }));
      throw new CatalogError(response.status, p.code, p.title);
    }
    return response.status === 204
      ? (undefined as T)
      : ((await response.json()) as T);
  }
}
