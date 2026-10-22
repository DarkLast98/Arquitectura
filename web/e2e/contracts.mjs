import SwaggerParser from "@apidevtools/swagger-parser";
import Ajv from "ajv/dist/2020.js";
import addFormats from "ajv-formats";
import { readFileSync } from "node:fs";
import { fileURLToPath } from "node:url";
const contractPath = fileURLToPath(
  new URL("../../contracts/", import.meta.url),
);
const spec = JSON.parse(
  readFileSync(contractPath + "openapi/catalog.v1.json", "utf8"),
);
await SwaggerParser.validate(contractPath + "openapi/catalog.v1.json");
const schema = JSON.parse(
  readFileSync(
    contractPath + "schemas/catalog/0.1.0/catalog.schema.json",
    "utf8",
  ),
);
const ajv = new Ajv({ strict: true, allErrors: true });
addFormats(ajv);
ajv.addSchema(schema);
for (const name of Object.keys(schema.$defs))
  ajv.getSchema(schema.$id + "#/$defs/" + name);
const validate = ajv.getSchema(schema.$id + "#/$defs/CreateEntity");
if (
  !validate({
    type_id: "core.application",
    schema_version: "0.1.0",
    code: "APP-DEMO",
    name: "Aplicación",
  })
)
  throw new Error("Ejemplo válido rechazado");
for (const body of [
  { type_id: "arbitrary", schema_version: "0.1.0", code: "APP", name: "X" },
  {
    type_id: "core.application",
    schema_version: "0.1.0",
    code: "APP",
    name: "X",
    organization_id: "spoofed",
  },
  {
    type_id: "core.application",
    schema_version: "0.1.0",
    code: "APP",
    name: " ",
  },
])
  if (validate(body)) throw new Error("Entrada inválida aceptada");
for (const invalid of ["bad\0text", "\ud800", "\udfff"]) {
  if(validate({type_id:"core.application",schema_version:"0.1.0",code:"UNICODE",name:invalid})) throw new Error("Unsupported Unicode accepted in name");
  if(validate({type_id:"core.application",schema_version:"0.1.0",code:"UNICODE",name:"Valid",description:invalid})) throw new Error("Unsupported Unicode accepted in description");
}
const served=await fetch("https://localhost:7240/contracts/openapi/catalog.v1.json");
if(served.status!==200 || JSON.stringify(await served.json())!==JSON.stringify(spec)) throw new Error("Served OpenAPI differs from versioned contract");
const servedSchema=await fetch("https://localhost:7240/contracts/schemas/catalog/0.1.0/catalog.schema.json");
if(servedSchema.status!==200 || JSON.stringify(await servedSchema.json())!==JSON.stringify(schema)) throw new Error("Served JSON Schema differs from versioned contract");
const response = await fetch(
  "https://localhost:7240/api/v1/organizations/00000000-0000-4000-8000-00000000000a/catalog/entities",
);
const route =
  spec.paths["/api/v1/organizations/{organizationId}/catalog/entities"].get;
if (!route.responses[response.status]) throw new Error("Estado no documentado");
if (!ajv.getSchema(schema.$id + "#/$defs/Problem")(await response.json()))
  throw new Error("Error real no corresponde al contrato");
console.log(
  "OpenAPI 3.1 validado; " +
    Object.keys(schema.$defs).length +
    " esquemas compilados; ejemplos y respuesta real verificados.",
);
