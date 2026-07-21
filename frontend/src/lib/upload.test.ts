import { describe, expect, it } from "vitest";
import { MAX_UPLOAD_SIZE_BYTES, formatFileSize, validateUpload } from "./upload";

function file(name: string, size: number): File {
  const blob = new File([new ArrayBuffer(0)], name);
  Object.defineProperty(blob, "size", { value: size });
  return blob;
}

describe("validateUpload", () => {
  it("izin verilen uzantı ve boyutu kabul eder", () => {
    expect(validateUpload(file("rapor.pdf", 1024))).toBeNull();
    expect(validateUpload(file("EKRAN.PNG", MAX_UPLOAD_SIZE_BYTES))).toBeNull();
  });

  it("izin verilmeyen uzantıyı reddeder (backend listesi)", () => {
    expect(validateUpload(file("video.mov", 1024))).toContain("video.mov");
    expect(validateUpload(file("arsiv.zip", 1024))).not.toBeNull();
    expect(validateUpload(file("uzantisiz", 1024))).not.toBeNull();
  });

  it("5 MB üstünü reddeder", () => {
    expect(validateUpload(file("buyuk.pdf", MAX_UPLOAD_SIZE_BYTES + 1))).toContain(
      "5 MB",
    );
  });
});

describe("formatFileSize", () => {
  it("B/KB/MB birimlerini üretir", () => {
    expect(formatFileSize(512)).toBe("512 B");
    expect(formatFileSize(4096)).toBe("4 KB");
    expect(formatFileSize(2_202_009)).toMatch(/MB$/);
  });
});
