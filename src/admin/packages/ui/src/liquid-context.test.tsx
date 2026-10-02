import { describe, expect, it } from "vitest";
import { liquidContextAt } from "./liquid-context";

function at(template: string) {
  const pos = template.indexOf("‸");
  return liquidContextAt(template.slice(0, pos) + template.slice(pos + 1), pos);
}

describe("liquidContextAt", () => {
  it("is plain text outside Liquid delimiters", () => {
    expect(at("<p>Hello ‸</p>")).toEqual({ kind: "text" });
    expect(at("{{ Target.Title }} and ‸")).toEqual({ kind: "text" });
    expect(at("{% if x %}‸")).toEqual({ kind: "text" });
  });

  it("offers expressions right after {{ is opened", () => {
    expect(at("<h1>{{‸}}")).toEqual({ kind: "expression", from: 6, query: "", delimiter: "output", justOpened: true });
    expect(at("<h1>{{- ‸")).toMatchObject({ kind: "expression", query: "", justOpened: true });
  });

  it("tracks a dotted path being typed", () => {
    expect(at("{{ CurrentUser.Fir‸ }}")).toEqual({
      kind: "expression",
      from: 3,
      query: "CurrentUser.Fir",
      delimiter: "output",
      justOpened: false,
    });
  });

  it("is not just opened after a finished value", () => {
    expect(at("{{ Target.Title ‸")).toMatchObject({ kind: "expression", query: "", justOpened: false });
  });

  it("completes filters after a pipe", () => {
    expect(at('{{ "logo.png" | attach‸')).toEqual({ kind: "filter", from: 16, query: "attach" });
    expect(at("{{ Target.Title |‸")).toEqual({ kind: "filter", from: 17, query: "" });
  });

  it("completes tag names right after {%", () => {
    expect(at("{%‸%}")).toEqual({ kind: "tag-name", from: 2, query: "" });
    expect(at("{%- fo‸")).toEqual({ kind: "tag-name", from: 4, query: "fo" });
  });

  it("completes expressions inside a tag after its name", () => {
    expect(at("{% for item in Target.It‸ %}")).toMatchObject({
      kind: "expression",
      query: "Target.It",
      delimiter: "tag",
      justOpened: false,
    });
  });

  it("stays quiet inside string literals", () => {
    expect(at('{{ "some-ke‸ }}')).toEqual({ kind: "text" });
    expect(at("{% assign x = 'a‸ %}")).toEqual({ kind: "text" });
  });
});
