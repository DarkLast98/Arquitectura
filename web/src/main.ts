import { bootstrapApplication } from "@angular/platform-browser";
import { catalogProvider } from "./app/composition";
import { App } from "./app/catalog-app";
bootstrapApplication(App, { providers: [catalogProvider] }).catch(() =>
  console.error("No se pudo iniciar AIP"),
);
