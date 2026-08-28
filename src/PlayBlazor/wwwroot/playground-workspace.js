// Layout gestures for PlaygroundWorkspace (Concept G v2): header drag-to-dock/float,
// float resize, zone grips, document keys, localStorage persistence. All state lives
// in .NET (WorkspaceLayout); this module only reports gestures and paints transients.

const STORAGE_KEY = "pb-workspace-v1";
let ctx = null;

export function init(dotnetRef) {
    dispose();
    const root = document.querySelector(".pbw");
    if (!root) {
        return loadLayout();
    }

    ctx = { root, ref: dotnetRef, cleanup: [] };
    const on = (target, type, handler, opts) => {
        target.addEventListener(type, handler, opts);
        ctx.cleanup.push(() => target.removeEventListener(type, handler, opts));
    };

    on(root, "pointerdown", e => {
        const head = e.target.closest(".pbw-panel-head");
        if (head && !e.target.closest("button")) {
            startPanelDrag(e, head.closest(".pbw-panel"));
            return;
        }
        const rz = e.target.closest(".pbw-rz");
        if (rz) {
            startFloatResize(e, rz);
            return;
        }
        const grip = e.target.closest(".pbw-zgrip");
        if (grip) {
            startZoneResize(e, grip);
        }
    });

    on(document, "keydown", e => {
        const inField = /^(input|select|textarea)$/i.test(document.activeElement?.tagName ?? "");
        // Arrow keys walk the graph tree when a node has focus (spec: ↑↓ walk, Enter selects).
        if ((e.key === "ArrowDown" || e.key === "ArrowUp") && document.activeElement?.closest?.("[data-panel=graph]")) {
            const nodes = [...document.querySelectorAll("[data-panel=graph] button.pbw-tnode")];
            const index = nodes.indexOf(document.activeElement.closest("button.pbw-tnode"));
            const next = nodes[index + (e.key === "ArrowDown" ? 1 : -1)];
            if (next) {
                e.preventDefault();
                next.focus();
            }
            return;
        }
        if (!inField && e.key >= "1" && e.key <= "4") {
            ctx.ref.invokeMethodAsync("OnKey", e.key);
        } else if (!inField && e.key === "/") {
            e.preventDefault();
            ctx.ref.invokeMethodAsync("OnKey", "/");
            focusFilter(6);
        } else if (e.key === "Escape" || e.key === "ArrowLeft" || e.key === "ArrowRight") {
            ctx.ref.invokeMethodAsync("OnKey", e.key);
        }
    });

    on(window, "resize", () => clampFloats());

    return loadLayout();
}

export function saveLayout(json) {
    try { localStorage.setItem(STORAGE_KEY, json); } catch { /* private mode */ }
}

export function loadLayout() {
    try { return localStorage.getItem(STORAGE_KEY); } catch { return null; }
}

export function dispose() {
    ctx?.cleanup.forEach(f => f());
    ctx = null;
}

function zones() {
    return [...ctx.root.querySelectorAll(".pbw-zone")];
}

function zoneName(zone) {
    return zone.classList.contains("pbw-zone-right") ? "right" : "bottom";
}

function zoneHit(zone, x, y) {
    const r = zone.getBoundingClientRect();
    return x >= r.left - 20 && x <= r.right + 20 && y >= r.top - 20 && y <= r.bottom + 20;
}

function focusFilter(retries) {
    const input = document.querySelector(".pbw-pfilter");
    if (input && input.offsetParent !== null) {
        input.focus();
    } else if (retries > 0) {
        // The panel may still be re-rendering (it was hidden or collapsed).
        setTimeout(() => focusFilter(retries - 1), 80);
    }
}

function track(e, onMove, onUp, onCancel) {
    e.preventDefault();
    // Capture keeps pointerup coming even when the pointer leaves the window —
    // without it, releasing outside strands the gesture (a ghost panel, stuck zones).
    try { e.target.setPointerCapture?.(e.pointerId); } catch { /* detached target */ }
    const move = ev => onMove(ev);
    const done = handler => ev => {
        window.removeEventListener("pointermove", move);
        window.removeEventListener("pointerup", up);
        window.removeEventListener("pointercancel", cancel);
        window.removeEventListener("keydown", key);
        handler(ev);
    };
    const up = done(onUp);
    const cancel = done(ev => (onCancel ?? onUp)(ev));
    const key = ev => {
        if (ev.key === "Escape") {
            cancel(ev);
        }
    };
    window.addEventListener("pointermove", move);
    window.addEventListener("pointerup", up);
    window.addEventListener("pointercancel", cancel);
    window.addEventListener("keydown", key);
}

function startPanelDrag(e, panel) {
    const id = panel.dataset.panel;
    const startX = e.clientX, startY = e.clientY;
    const rect = panel.getBoundingClientRect();
    const offX = Math.min(e.clientX - rect.left, 320), offY = e.clientY - rect.top;
    let ghost = null;

    track(e, ev => {
        if (!ghost) {
            if (Math.hypot(ev.clientX - startX, ev.clientY - startY) < 5) {
                return;
            }
            ghost = panel.cloneNode(true);
            ghost.classList.add("pbw-panel-float", "pbw-panel-dragging");
            ghost.style.width = "350px";
            ghost.style.height = "";
            document.body.appendChild(ghost);
            zones().forEach(z => z.classList.add("pbw-zone-droptarget"));
        }
        ghost.style.left = ev.clientX - offX + "px";
        ghost.style.top = ev.clientY - offY + "px";
        zones().forEach(z => z.classList.toggle("pbw-zone-hot", zoneHit(z, ev.clientX, ev.clientY)));
    }, ev => {
        zones().forEach(z => z.classList.remove("pbw-zone-droptarget", "pbw-zone-hot"));
        if (!ghost) {
            return;
        }
        ghost.remove();
        const zone = zones().find(z => zoneHit(z, ev.clientX, ev.clientY));
        if (zone) {
            const siblings = [...zone.querySelectorAll("[data-panel]")].filter(p => p.dataset.panel !== id);
            const horizontal = zoneName(zone) === "bottom";
            let index = siblings.length;
            for (let i = 0; i < siblings.length; i++) {
                const r = siblings[i].getBoundingClientRect();
                const center = horizontal ? r.left + r.width / 2 : r.top + r.height / 2;
                if ((horizontal ? ev.clientX : ev.clientY) < center) {
                    index = i;
                    break;
                }
            }
            ctx.ref.invokeMethodAsync("OnPanelDropped", id, zoneName(zone), index, 0, 0);
        } else {
            const x = Math.max(0, Math.min(window.innerWidth - 80, ev.clientX - offX));
            const y = Math.max(52, Math.min(window.innerHeight - 40, ev.clientY - offY));
            ctx.ref.invokeMethodAsync("OnPanelDropped", id, null, 0, x, y);
        }
    }, () => {
        // Cancelled drag (Escape, pointer lost): clean the transients, change nothing.
        zones().forEach(z => z.classList.remove("pbw-zone-droptarget", "pbw-zone-hot"));
        ghost?.remove();
    });
}

function startFloatResize(e, handle) {
    const panel = handle.closest(".pbw-panel");
    const id = panel.dataset.panel;
    const rect = panel.getBoundingClientRect();
    const resizeX = handle.classList.contains("pbw-rz-r") || handle.classList.contains("pbw-rz-c");
    const resizeY = handle.classList.contains("pbw-rz-b") || handle.classList.contains("pbw-rz-c");
    let w = rect.width, h = rect.height;
    e.stopPropagation();

    track(e, ev => {
        if (resizeX) {
            w = Math.max(250, Math.min(760, ev.clientX - rect.left));
            panel.style.width = w + "px";
        }
        if (resizeY) {
            h = Math.max(130, Math.min(window.innerHeight * 0.8, ev.clientY - rect.top));
            panel.style.height = h + "px";
            panel.style.maxHeight = "none";
        }
    }, () => ctx.ref.invokeMethodAsync("OnFloatResized", id, w, h));
}

function startZoneResize(e, grip) {
    const bottom = grip.classList.contains("pbw-zgrip-bottom");
    const zone = grip.closest(".pbw-zone");
    const start = bottom ? zone.getBoundingClientRect().height : zone.getBoundingClientRect().width;
    const startX = e.clientX, startY = e.clientY;
    let value = start;
    e.stopPropagation();

    track(e, ev => {
        value = bottom
            ? Math.max(120, Math.min(520, start + (startY - ev.clientY)))
            : Math.max(240, Math.min(560, start + (startX - ev.clientX)));
        ctx.root.style.setProperty(bottom ? "--pbw-bottom" : "--pbw-right", value + "px");
    }, () => ctx.ref.invokeMethodAsync("OnZoneResized", bottom ? "bottom" : "right", value));
}

function clampFloats() {
    ctx.root.ownerDocument.querySelectorAll(".pbw-panel-float:not(.pbw-panel-dragging)").forEach(panel => {
        const r = panel.getBoundingClientRect();
        const x = Math.max(0, Math.min(window.innerWidth - 80, r.left));
        const y = Math.max(52, Math.min(window.innerHeight - 40, r.top));
        if (x !== r.left || y !== r.top) {
            ctx.ref.invokeMethodAsync("OnPanelDropped", panel.dataset.panel, null, 0, x, y);
        }
    });
}
