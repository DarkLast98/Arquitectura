import { CATALOG } from "./catalog-token";
import { CatalogWorkspace } from "./application/catalog-workspace";
import { HttpCatalogGateway } from "./adapters/http-catalog-gateway";
export const catalogProvider = {
  provide: CATALOG,
  useFactory: () => new CatalogWorkspace(new HttpCatalogGateway()),
};
