import {renderToStaticMarkup} from "react-dom/server";
import {examples, element} from "./examples.mjs";

export {examples};

export function render(example) {
  return renderToStaticMarkup(element(example));
}
