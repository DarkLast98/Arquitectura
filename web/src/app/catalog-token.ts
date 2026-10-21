import { InjectionToken } from "@angular/core";
import type { CatalogWorkspace } from "./application/catalog-workspace";
export const CATALOG = new InjectionToken<CatalogWorkspace>("CatalogWorkspace");
