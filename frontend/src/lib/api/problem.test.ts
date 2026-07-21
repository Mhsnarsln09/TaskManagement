import { describe, expect, it } from "vitest";
import { ApiError, fieldErrors, problemMessage } from "./problem";

describe("fieldErrors", () => {
  it("PascalCase ve camelCase anahtarları alanla eşler", () => {
    const problem = {
      status: 400,
      errors: { Title: ["Başlık zorunludur."], dueDate: ["Geçersiz tarih."] },
    };
    expect(fieldErrors(problem, "title")).toEqual(["Başlık zorunludur."]);
    expect(fieldErrors(problem, "DueDate")).toEqual(["Geçersiz tarih."]);
    expect(fieldErrors(problem, "unknown")).toEqual([]);
  });
});

describe("problemMessage", () => {
  it("detail > title > fallback önceliğiyle mesaj üretir", () => {
    expect(
      problemMessage(new ApiError(409, { title: "Conflict", detail: "Çakıştı." })),
    ).toBe("Çakıştı.");
    expect(problemMessage(new ApiError(500, { title: "Server error" }))).toBe(
      "Server error",
    );
    expect(problemMessage(new Error("x"))).toBe("Beklenmeyen bir hata oluştu.");
  });
});
