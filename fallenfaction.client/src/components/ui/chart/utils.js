import { isClient } from "@vueuse/core";
import { h, render } from "vue";
const cache = /* @__PURE__ */ new Map();
function serializeKey(key) {
  return JSON.stringify(key, Object.keys(key).sort());
}
export function componentToString(config, component, props) {
  if (!isClient)
    return;
  const id = useId();
  return (_data, x) => {
    const data = "data" in _data ? _data.data : _data;
    const serializedKey = `${id}-${serializeKey(data)}`;
    const cachedContent = cache.get(serializedKey);
    if (cachedContent)
      return cachedContent;
    const vnode = h(component, { ...props, payload: data, config, x });
    const div = document.createElement("div");
    render(vnode, div);
    cache.set(serializedKey, div.innerHTML);
    return div.innerHTML;
  };
}
