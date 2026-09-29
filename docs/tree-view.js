// ========================
// TREE VIEW — визуализация дерева разбора (D3.js)
// Открывается кнопкой View Tree, использует JSON, который уже
// пришёл в ответе /translate вместе с cppCode (поле data.tree).
// ========================
const TreeView = (function () {
  "use strict";

  const screen = document.getElementById("treeScreen");
  const container = document.getElementById("treeContainer");
  const closeBtn = document.getElementById("closeTreeBtn");
  const expandBtn = document.getElementById("treeExpandBtn");
  const collapseBtn = document.getElementById("treeCollapseBtn");
  const fitBtn = document.getElementById("treeFitBtn");

  const LEVEL_HEIGHT = 110; // вертикальный шаг между уровнями
  const UNIT = 20;          // базовая единица для separation()
  const NODE_H = 34;        // высота коробки узла
  const PAD_X = 16;         // внутренний отступ текста в коробке
  const GAP = 24;           // минимальный зазор между соседними узлами

  const measureCanvas = document.createElement("canvas");
  const mctx = measureCanvas.getContext("2d");
  function measure(text, font) {
    mctx.font = font;
    return mctx.measureText(text).width;
  }
  function isToken(name) { return typeof name === "string" && name.startsWith("'"); }

  let svg, g, zoomBehavior, root, nodeIdCounter = 0;

  function initSvg() {
    d3.select(container).select("svg").remove();
    const width = container.clientWidth;
    const height = container.clientHeight;

    svg = d3.select(container).append("svg")
      .attr("width", width)
      .attr("height", height);

    g = svg.append("g");

    zoomBehavior = d3.zoom()
      .scaleExtent([0.08, 3])
      .on("zoom", (event) => g.attr("transform", event.transform));

    svg.call(zoomBehavior).on("dblclick.zoom", null);
  }

  function diagonal(d) {
    const sx = d.source.x, sy = d.source.y + NODE_H / 2;
    const tx = d.target.x, ty = d.target.y - NODE_H / 2;
    const my = (sy + ty) / 2;
    return `M${sx},${sy} C${sx},${my} ${tx},${my} ${tx},${ty}`;
  }

  function collapseDeep(d) {
    if (d.children) {
      d._children = d.children;
      d._children.forEach(collapseDeep);
      d.children = null;
    }
  }

  function toggle(d) {
    if (!d.children && !d._children) return;
    if (d.children) { d._children = d.children; d.children = null; }
    else { d.children = d._children; d._children = null; }
  }

  function update(source) {
    const nodes = root.descendants();
    const links = root.links();

    nodes.forEach(d => {
      const token = isToken(d.data.name);
      const font = token ? '600 13px "SF Mono", "Fira Code", monospace' : '500 13px "Inter"';
      d._w = Math.max(34, measure(String(d.data.name), font) + PAD_X * 2);
      d._token = token;
    });

    const layout = d3.tree()
      .nodeSize([UNIT, LEVEL_HEIGHT])
      .separation((a, b) => (a._w / 2 + b._w / 2 + GAP) / UNIT);
    layout(root);

    // ---- узлы ----
    const node = g.selectAll("g.tree-node").data(nodes, d => d.id || (d.id = ++nodeIdCounter));

    const nodeEnter = node.enter().append("g")
      .attr("class", d => "tree-node " + (d._token ? "token" : "rule"))
      .attr("transform", () => `translate(${source.x0 ?? source.x ?? 0},${source.y0 ?? source.y ?? 0})`)
      .style("opacity", 0)
      .on("click", (event, d) => { toggle(d); update(d); });

    nodeEnter.append("title")
      .text(d => (d.children || d._children) ? "Клик — свернуть/развернуть" : d.data.name);

    nodeEnter.append("rect")
      .attr("rx", d => d._token ? 14 : 7)
      .attr("ry", d => d._token ? 14 : 7)
      .attr("x", d => -d._w / 2)
      .attr("y", -NODE_H / 2)
      .attr("width", d => d._w)
      .attr("height", NODE_H);

    nodeEnter.append("text")
      .attr("dy", "0.32em")
      .attr("text-anchor", "middle")
      .text(d => d.data.name);

    const nodeUpdate = nodeEnter.merge(node);

    nodeUpdate.classed("collapsed", d => !!d._children);

    nodeUpdate.transition().duration(350)
      .attr("transform", d => `translate(${d.x},${d.y})`)
      .style("opacity", 1);

    node.exit().transition().duration(250)
      .attr("transform", () => `translate(${source.x},${source.y})`)
      .style("opacity", 0)
      .remove();

    // ---- связи ----
    const link = g.selectAll("path.tree-link").data(links, d => d.target.id);

    const linkEnter = link.enter().insert("path", "g")
      .attr("class", d => "tree-link" + (d.target._token ? " token" : ""))
      .attr("d", () => {
        const o = { x: source.x0 ?? source.x ?? 0, y: source.y0 ?? source.y ?? 0 };
        return diagonal({ source: o, target: o });
      });

    linkEnter.merge(link).transition().duration(350).attr("d", diagonal);

    link.exit().transition().duration(250)
      .attr("d", () => {
        const o = { x: source.x, y: source.y };
        return diagonal({ source: o, target: o });
      })
      .remove();

    nodes.forEach(d => { d.x0 = d.x; d.y0 = d.y; });
  }

  function fitToScreen(animate) {
    if (!g || g.selectAll("g.tree-node").empty()) return;
    const bounds = g.node().getBBox();
    const width = container.clientWidth, height = container.clientHeight;
    if (bounds.width === 0 || bounds.height === 0) return;

    const scale = Math.min(
      Math.min((width * 0.9) / bounds.width, (height * 0.9) / bounds.height),
      1.3
    );
    const tx = width / 2 - (bounds.x + bounds.width / 2) * scale;
    const ty = 36 - bounds.y * scale;

    const target = d3.zoomIdentity.translate(tx, ty).scale(scale);
    if (animate === false) svg.call(zoomBehavior.transform, target);
    else svg.transition().duration(400).call(zoomBehavior.transform, target);
  }

  function renderTree(data) {
    if (!data) return;
    initSvg();
    nodeIdCounter = 0;

    root = d3.hierarchy(data, d => (d.children && d.children.length ? d.children : null));
    root.x0 = 0;
    root.y0 = 0;

    update(root);
    requestAnimationFrame(() => fitToScreen(false));
  }

  function expandAll() {
    if (!root) return;
    root.each(d => { if (d._children) { d.children = d._children; d._children = null; } });
    update(root);
    fitToScreen(true);
  }

  function collapseToRoot() {
    if (!root) return;
    if (root.children) root.children.forEach(collapseDeep);
    update(root);
    fitToScreen(true);
  }

  function open(data) {
    screen.classList.add("show");
    renderTree(data);
  }

  function close() {
    screen.classList.remove("show");
  }

  closeBtn.addEventListener("click", close);
  expandBtn.addEventListener("click", expandAll);
  collapseBtn.addEventListener("click", collapseToRoot);
  fitBtn.addEventListener("click", () => fitToScreen(true));

  document.addEventListener("keydown", (e) => {
    if (e.key === "Escape" && screen.classList.contains("show")) close();
  });

  window.addEventListener("resize", () => {
    if (!svg || !screen.classList.contains("show")) return;
    svg.attr("width", container.clientWidth).attr("height", container.clientHeight);
    fitToScreen(false);
  });

  return { open, close, expandAll, collapseToRoot, fitToScreen };
})();
