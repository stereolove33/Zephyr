import { toSlug } from "@/utils/slug";

describe("toSlug", () => {
  it("converts a simple display name", () => {
    expect(toSlug("My Awesome Mod")).toBe("my-awesome-mod");
  });

  it("returns empty string for empty input", () => {
    expect(toSlug("")).toBe("");
  });

  it("strips leading and trailing hyphens", () => {
    expect(toSlug("--hello--")).toBe("hello");
    expect(toSlug("  hello  ")).toBe("hello");
  });

  it("collapses consecutive non-alphanumeric characters into a single hyphen", () => {
    expect(toSlug("a   b")).toBe("a-b");
    expect(toSlug("a---b")).toBe("a-b");
    expect(toSlug("a @#$ b")).toBe("a-b");
  });

  it("lowercases uppercase characters", () => {
    expect(toSlug("HELLO WORLD")).toBe("hello-world");
    expect(toSlug("CamelCase")).toBe("camelcase");
  });

  it("preserves numbers", () => {
    expect(toSlug("Version 2 Beta")).toBe("version-2-beta");
    expect(toSlug("123abc")).toBe("123abc");
  });

  it("strips diacritics via NFKD normalization", () => {
    expect(toSlug("Côte d'Azur")).toBe("cote-d-azur");
    expect(toSlug("über cool")).toBe("uber-cool");
    expect(toSlug("résumé")).toBe("resume");
    expect(toSlug("naïve café")).toBe("naive-cafe");
  });

  it("handles non-Latin characters by stripping them", () => {
    expect(toSlug("日本語テスト")).toBe("");
    expect(toSlug("hello 世界")).toBe("hello");
  });

  it("handles strings that are only whitespace or special characters", () => {
    expect(toSlug("   ")).toBe("");
    expect(toSlug("@#$%^&*")).toBe("");
    expect(toSlug("---")).toBe("");
  });

  it("handles single character input", () => {
    expect(toSlug("a")).toBe("a");
    expect(toSlug("A")).toBe("a");
    expect(toSlug("-")).toBe("");
  });
});
