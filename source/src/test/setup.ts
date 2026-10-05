import "@testing-library/jest-dom/vitest";
import "./mocks/tauri";

const originalNumberFormat = Intl.NumberFormat;

Intl.NumberFormat = new Proxy(originalNumberFormat, {
  construct(target, args) {
    return Reflect.construct(target, [args[0] ?? "en-US", args[1]]);
  },
  apply(target, thisArg, args) {
    return Reflect.apply(target, thisArg, [args[0] ?? "en-US", args[1]]);
  },
});

const originalNumberToLocaleString = Number.prototype.toLocaleString;

Number.prototype.toLocaleString = function (locales, options) {
  return originalNumberToLocaleString.call(this, locales ?? "en-US", options);
};
