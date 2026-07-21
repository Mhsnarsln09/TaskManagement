// .NET 10 OpenAPI üreticisi, Http.Json'ın AllowReadingFromString davranışı
// nedeniyle sayısal alanları ["integer","string"] birleşimi olarak yayınlar.
// Runtime yanıtları her zaman sayıdır; tip üretiminden önce normalize edilir.
import { readFileSync, writeFileSync } from "node:fs";

const path = new URL("../src/lib/api/openapi.json", import.meta.url);
const document = JSON.parse(readFileSync(path, "utf8"));

function walk(node) {
  if (Array.isArray(node)) {
    for (const item of node) walk(item);
    return;
  }
  if (node === null || typeof node !== "object") return;
  if (Array.isArray(node.type)) {
    const numeric = node.type.find((t) => t === "integer" || t === "number");
    if (numeric && node.type.includes("string")) {
      node.type = numeric;
      delete node.pattern;
    }
  }
  for (const value of Object.values(node)) walk(value);
}

walk(document);
writeFileSync(path, JSON.stringify(document, null, 2) + "\n");
console.log("openapi.json normalized");
