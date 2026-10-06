export function applyCustomOrder(items, saved) {
  const rank = new Map();
  if (Array.isArray(saved)) for (const id of saved) if (typeof id === "string" && !rank.has(id)) rank.set(id, rank.size);
  return items.map((item, index) => ({ item, index }))
    .sort((a, b) => (rank.get(a.item.id) ?? Infinity) - (rank.get(b.item.id) ?? Infinity) || a.index - b.index)
    .map(entry => entry.item);
}

export function moveCustomOrder(ids, source, target, after = false) {
  if (source === target || !ids.includes(source) || !ids.includes(target)) return ids.slice();
  const result = ids.filter(id => id !== source);
  result.splice(result.indexOf(target) + (after ? 1 : 0), 0, source);
  return result;
}
