export function stableIds(markup) {
  const families = new Map();
  return markup.replace(/recharts((?:-[a-z]+)*-)?(\d+)/g, (match, family = "", number) => {
    if (family === "-treemap-depth-") return match;
    let ids = families.get(family);
    if (!ids) families.set(family, ids = new Map());
    if (!ids.has(number)) ids.set(number, ids.size + 1);
    return `recharts${family}${ids.get(number)}`;
  });
}
