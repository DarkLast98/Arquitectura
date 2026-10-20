import { bootstrapApplication } from "@angular/platform-browser";
import { App } from "./app/catalog-app";
bootstrapApplication(App).catch(() => console.error("No se pudo iniciar AIP"));
