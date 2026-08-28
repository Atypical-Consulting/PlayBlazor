/* Shared model for round-2 concepts: two components (simple + stress case),
   live-wired specimen rendering, and razor snippet generation. */

const PB_COLORS = { Default: "#8e8e9a", Primary: "#594ae2", Secondary: "#ff4081", Tertiary: "#1ec8a5", Info: "#2196f3", Success: "#00c853", Warning: "#ff9800", Error: "#f44336", Dark: "#424242" };
const PB_SIZES = { Small: [".375rem .625rem", ".75rem"], Medium: [".5rem 1rem", ".875rem"], Large: [".7rem 1.4rem", ".9375rem"] };

const PB_PERSONS = [
    ["Elena Vasquez", 34, "Lisbon"],
    ["Marc Dubois", 41, "Brussels"],
    ["Aiko Tanaka", 28, "Osaka"],
    ["Jonas Berg", 52, "Oslo"],
    ["Priya Sharma", 37, "Pune"],
    ["Tom Weiss", 45, "Vienna"],
];

/* p: [name, group, kind, default, options?]  kind: bool|enum|number|string|preset|slot|event */
const PB_COMPONENTS = {
    "MudButton": {
        label: "MudButton",
        presets: [
            ["Filled primary", { Variant: "Filled", Color: "Primary" }],
            ["Outlined", { Variant: "Outlined", Color: "Secondary" }],
            ["Text", { Variant: "Text", Color: "Primary" }],
            ["Disabled", { Variant: "Filled", Color: "Primary", Disabled: true }],
        ],
        seed: { Variant: "Filled", Color: "Primary" },
        params: [
            ["Variant", "Appearance", "enum", "Text", ["Text", "Filled", "Outlined"]],
            ["Color", "Appearance", "enum", "Default", Object.keys(PB_COLORS)],
            ["Size", "Appearance", "enum", "Medium", ["Small", "Medium", "Large"]],
            ["DropShadow", "Appearance", "bool", true],
            ["Disabled", "Behavior", "bool", false],
            ["Ripple", "Behavior", "bool", true],
            ["FullWidth", "Behavior", "bool", false],
            ["ChildContent", "Content", "string", "Click me"],
            ["OnClick", "Events", "event"],
        ],
    },
    "MudDataGrid<Person>": {
        label: "MudDataGrid<Person>",
        presets: [
            ["Compact striped", { Dense: true, Striped: true }],
            ["Bordered", { Bordered: true, Outlined: true }],
            ["Fixed header", { FixedHeader: true, Height: "220px" }],
            ["Loading", { Loading: true }],
            ["Multi-select", { MultiSelection: true }],
        ],
        seed: {},
        params: [
            ["Dense", "Appearance", "bool", false],
            ["Striped", "Appearance", "bool", false],
            ["Bordered", "Appearance", "bool", false],
            ["Hover", "Appearance", "bool", true],
            ["Outlined", "Appearance", "bool", false],
            ["Square", "Appearance", "bool", false],
            ["Elevation", "Appearance", "number", 1],
            ["FixedHeader", "Appearance", "bool", false],
            ["Height", "Appearance", "string", ""],
            ["HorizontalScrollbar", "Appearance", "bool", false],
            ["ShowMenuIcon", "Appearance", "bool", false],
            ["Loading", "Appearance", "bool", false],
            ["LoadingProgressColor", "Appearance", "enum", "Info", ["Primary", "Info", "Success", "Warning"]],
            ["ReadOnly", "Editing", "bool", true],
            ["EditMode", "Editing", "enum", "Cell", ["Cell", "Form"]],
            ["EditTrigger", "Editing", "enum", "RowClick", ["RowClick", "EditButton"]],
            ["CommitEditIcon", "Editing", "string", ""],
            ["CancelEditIcon", "Editing", "string", ""],
            ["Filterable", "Filtering", "bool", false],
            ["FilterMode", "Filtering", "enum", "Simple", ["Simple", "ColumnFilterMenu", "ColumnFilterRow"]],
            ["ShowFilterIcons", "Filtering", "bool", true],
            ["SortMode", "Sorting", "enum", "Single", ["None", "Single", "Multiple"]],
            ["Groupable", "Grouping", "bool", false],
            ["GroupExpanded", "Grouping", "bool", true],
            ["MultiSelection", "Selection", "bool", false],
            ["SelectOnRowClick", "Selection", "bool", true],
            ["RowsPerPage", "Paging", "number", 10],
            ["ColumnResizeMode", "Resizing", "enum", "None", ["None", "Column", "Container"]],
            ["DragDropColumnReordering", "Resizing", "bool", false],
            ["ShowColumnOptions", "Columns", "bool", true],
            ["Hideable", "Columns", "bool", false],
            ["RowClass", "Styling", "string", ""],
            ["RowStyle", "Styling", "string", ""],
            ["Items", "Data", "preset", "@_people — 6 rows"],
            ["Columns", "Data", "slot", "3 × PropertyColumn"],
            ["RowClick", "Events", "event"],
            ["SelectedItemChanged", "Events", "event"],
        ],
        /* Scaffold graph for tree-based concepts */
        tree: [
            { id: "grid", label: "MudDataGrid", tag: "MudDataGrid T=\"Person\"", depth: 0 },
            { id: "columns", label: "Columns", tag: "Columns", depth: 1 },
            { id: "col-name", label: "PropertyColumn · Name", tag: "PropertyColumn Property=\"x => x.Name\"", depth: 2, col: 0 },
            { id: "col-age", label: "PropertyColumn · Age", tag: "PropertyColumn Property=\"x => x.Age\"", depth: 2, col: 1 },
            { id: "col-city", label: "PropertyColumn · City", tag: "PropertyColumn Property=\"x => x.City\"", depth: 2, col: 2 },
        ],
        columnParams: [
            ["Title", "Appearance", "string", ""],
            ["Property", "Data", "preset", "x => x.…"],
            ["Sortable", "Behavior", "bool", true],
            ["Filterable", "Behavior", "bool", true],
            ["Resizable", "Behavior", "bool", true],
            ["Hidden", "Behavior", "bool", false],
            ["HeaderClass", "Styling", "string", ""],
            ["CellClass", "Styling", "string", ""],
        ],
    },
};

function pbMakeState(compKey) {
    const s = {};
    for (const [name, , kind, def] of PB_COMPONENTS[compKey].params)
        if (kind !== "event" && kind !== "slot" && kind !== "preset") s[name] = def;
    return Object.assign(s, PB_COMPONENTS[compKey].seed);
}

function pbGroups(compKey) {
    const seen = [];
    for (const [, g] of PB_COMPONENTS[compKey].params) if (!seen.includes(g)) seen.push(g);
    return seen;
}

function pbModified(compKey, state) {
    return PB_COMPONENTS[compKey].params
        .filter(([n, , k, d]) => k !== "event" && k !== "slot" && k !== "preset" && String(state[n]) !== String(d))
        .map(([n]) => n);
}

function pbAttrList(compKey, state) {
    const out = [];
    for (const [name, , kind, def, options] of PB_COMPONENTS[compKey].params) {
        if (kind === "event" || kind === "slot" || kind === "preset" || name === "ChildContent") continue;
        if (String(state[name]) === String(def)) continue;
        let v;
        if (kind === "bool") v = state[name] ? "true" : "false";
        else if (kind === "enum") v = { Variant: "Variant.", Color: "Color.", Size: "Size." }[name] !== undefined
            ? name + "." + state[name] : name.includes("Color") ? "Color." + state[name] : (options ? pbEnumPrefix(name) + state[name] : state[name]);
        else v = String(state[name]);
        out.push([name, v]);
    }
    return out;
}
function pbEnumPrefix(name) {
    return { EditMode: "DataGridEditMode.", EditTrigger: "DataGridEditTrigger.", FilterMode: "DataGridFilterMode.", SortMode: "SortMode.", ColumnResizeMode: "ResizeMode.", LoadingProgressColor: "Color." }[name] || "";
}

const PB_TOK = {
    p: s => `<span class="tk-p">${s}</span>`,
    tag: s => `<span class="tk-tag">${s}</span>`,
    attr: s => `<span class="tk-attr">${s}</span>`,
    val: s => `<span class="tk-val">${s}</span>`,
};

/* Returns array of {html, node} lines; node ids allow per-line highlight in tree concepts. */
function pbSnippetLines(compKey, state) {
    const t = PB_TOK;
    if (compKey === "MudButton") {
        const attrs = pbAttrList(compKey, state).map(([n, v]) => ` ${t.attr(n)}${t.p("=")}${t.val('"' + v + '"')}`).join("");
        return [{ html: `${t.p("&lt;")}${t.tag("MudButton")}${attrs}${t.p("&gt;")}${state.ChildContent}${t.p("&lt;/")}${t.tag("MudButton")}${t.p("&gt;")}`, node: "root" }];
    }
    const attrs = pbAttrList(compKey, state);
    const lines = [];
    let head = `${t.p("&lt;")}${t.tag("MudDataGrid")} ${t.attr("T")}${t.p("=")}${t.val('"Person"')} ${t.attr("Items")}${t.p("=")}${t.val('"@_people"')}`;
    if (attrs.length <= 2) {
        head += attrs.map(([n, v]) => ` ${t.attr(n)}${t.p("=")}${t.val('"' + v + '"')}`).join("") + t.p("&gt;");
        lines.push({ html: head, node: "grid" });
    } else {
        lines.push({ html: head, node: "grid" });
        attrs.forEach(([n, v], i) => lines.push({
            html: `                ${t.attr(n)}${t.p("=")}${t.val('"' + v + '"')}` + (i === attrs.length - 1 ? t.p("&gt;") : ""),
            node: "grid",
        }));
    }
    lines.push({ html: `    ${t.p("&lt;")}${t.tag("Columns")}${t.p("&gt;")}`, node: "columns" });
    for (const [prop, id] of [["Name", "col-name"], ["Age", "col-age"], ["City", "col-city"]])
        lines.push({ html: `        ${t.p("&lt;")}${t.tag("PropertyColumn")} ${t.attr("Property")}${t.p("=")}${t.val('"x => x.' + prop + '"')} ${t.p("/&gt;")}`, node: id });
    lines.push({ html: `    ${t.p("&lt;/")}${t.tag("Columns")}${t.p("&gt;")}`, node: "columns" });
    lines.push({ html: `${t.p("&lt;/")}${t.tag("MudDataGrid")}${t.p("&gt;")}`, node: "grid" });
    return lines;
}

/* ── Specimen renderers ─────────────────────────────────
   Both return a root element; grid re-renders in place. */

function pbRenderButton(state) {
    const b = document.createElement("button");
    b.className = "pbm-btn";
    const c = PB_COLORS[state.Color];
    const [pad, fs] = PB_SIZES[state.Size];
    b.textContent = state.ChildContent || "Click me";
    b.disabled = state.Disabled;
    b.style.padding = pad;
    b.style.fontSize = fs;
    if (state.FullWidth) b.style.width = "100%";
    if (state.Variant === "Filled") {
        b.style.background = c; b.style.color = "#fff";
        b.style.boxShadow = state.DropShadow && !state.Disabled ? "0 3px 8px -2px rgba(0,0,0,.35)" : "none";
    } else if (state.Variant === "Outlined") {
        b.style.background = "transparent"; b.style.color = c; b.style.borderColor = c;
    } else {
        b.style.background = "transparent"; b.style.color = c;
    }
    return b;
}

function pbRenderGrid(state, opts = {}) {
    const root = document.createElement("div");
    root.className = "pbm-grid";
    if (state.Outlined) root.classList.add("outlined");
    if (state.Square) root.classList.add("square");
    root.style.boxShadow = state.Outlined ? "none" :
        `0 ${state.Elevation}px ${state.Elevation * 3}px -${Math.max(1, state.Elevation)}px rgba(27,27,34,.3)`;
    if (state.Loading) {
        const bar = document.createElement("div");
        bar.className = "pbm-loading";
        bar.style.background = PB_COLORS[state.LoadingProgressColor] || PB_COLORS.Info;
        root.appendChild(bar);
    }
    const scroller = document.createElement("div");
    scroller.className = "pbm-scroll";
    if (state.FixedHeader) {
        scroller.classList.add("fixed");
        scroller.style.maxHeight = state.Height || "220px";
    }
    const table = document.createElement("table");
    table.className = "pbm-table";
    if (state.Dense) table.classList.add("dense");
    if (state.Striped) table.classList.add("striped");
    if (state.Bordered) table.classList.add("bordered");
    if (state.Hover) table.classList.add("hover");
    const cols = opts.columns || [{ label: "Name" }, { label: "Age" }, { label: "City" }];
    const thead = document.createElement("thead");
    const hr = document.createElement("tr");
    if (state.MultiSelection) hr.insertAdjacentHTML("beforeend", `<th class="pbm-check"><input type="checkbox" aria-label="Select all"></th>`);
    cols.forEach((c, i) => {
        if (c.hidden) return;
        const glyphs = (state.SortMode !== "None" ? " ↑" : "") + (state.Filterable && state.ShowFilterIcons ? " ⧩" : "") + (state.ShowMenuIcon ? " ⋮" : "");
        hr.insertAdjacentHTML("beforeend", `<th data-col="${i}">${c.label}<span class="pbm-glyph">${glyphs}</span></th>`);
    });
    thead.appendChild(hr);
    const tbody = document.createElement("tbody");
    for (const [name, age, city] of PB_PERSONS) {
        const tr = document.createElement("tr");
        if (state.MultiSelection) tr.insertAdjacentHTML("beforeend", `<td class="pbm-check"><input type="checkbox" aria-label="Select row"></td>`);
        [name, age, city].forEach((v, i) => { if (!cols[i].hidden) tr.insertAdjacentHTML("beforeend", `<td data-col="${i}">${v}</td>`); });
        if (opts.onRowClick) tr.addEventListener("click", () => opts.onRowClick(name));
        tbody.appendChild(tr);
    }
    table.append(thead, tbody);
    scroller.appendChild(table);
    root.appendChild(scroller);
    if (opts.highlightCol !== undefined && opts.highlightCol !== null)
        root.querySelectorAll(`[data-col="${opts.highlightCol}"]`).forEach(el => el.classList.add("pbm-hl"));
    return root;
}

/* Base CSS for specimens — inject once per page. */
function pbInjectSpecimenCss() {
    const css = `
.pbm-btn { font-family: system-ui, sans-serif; font-weight: 500; text-transform: uppercase; letter-spacing: .02em; border-radius: 4px; border: 1px solid transparent; cursor: pointer; }
.pbm-btn:disabled { background: rgba(0,0,0,.12) !important; color: rgba(0,0,0,.32) !important; border-color: transparent !important; box-shadow: none !important; cursor: default; }
.pbm-grid { position: relative; background: #fff; border-radius: 6px; overflow: hidden; min-width: 420px; max-width: 640px; font-family: system-ui, sans-serif; }
.pbm-grid.outlined { border: 1px solid #d5d5e0; }
.pbm-grid.square { border-radius: 0; }
.pbm-loading { height: 3px; width: 40%; border-radius: 2px; animation: pbm-slide 1.4s ease-in-out infinite; }
@keyframes pbm-slide { 0% { margin-left: 0; width: 10%; } 50% { margin-left: 60%; width: 40%; } 100% { margin-left: 100%; width: 10%; } }
.pbm-scroll.fixed { overflow-y: auto; }
.pbm-scroll.fixed thead th { position: sticky; top: 0; background: #fff; z-index: 1; }
.pbm-table { width: 100%; border-collapse: collapse; font-size: .8125rem; color: #2c2c36; }
.pbm-table th { text-align: left; font-weight: 600; font-size: .75rem; padding: .7rem .9rem; border-bottom: 2px solid #e6e6ee; white-space: nowrap; }
.pbm-table td { padding: .65rem .9rem; border-bottom: 1px solid #efeff5; }
.pbm-table.dense th { padding: .35rem .9rem; }
.pbm-table.dense td { padding: .3rem .9rem; }
.pbm-table.striped tbody tr:nth-child(even) { background: #f6f6fa; }
.pbm-table.bordered th, .pbm-table.bordered td { border: 1px solid #e6e6ee; }
.pbm-table.hover tbody tr:hover { background: #efeaff; }
.pbm-table .pbm-glyph { color: #9a9ab0; font-size: .6875rem; }
.pbm-check { width: 34px; }
.pbm-hl { background: rgba(109, 74, 255, .14) !important; box-shadow: inset 0 -2px 0 #6d4aff; }
@media (prefers-reduced-motion: reduce) { .pbm-loading { animation: none; } }`;
    const s = document.createElement("style");
    s.textContent = css;
    document.head.appendChild(s);
}
