// radar.js
(() => {
    const grid = document.getElementById("radarGrid");
    const log = document.getElementById("log");
    const clearBtn = document.getElementById("clearLog");

    const cards = new Map(); // nodeId -> elements
    const MAX_LOG_ENTRIES = 9;

    // ----- MAP -----
    let map = null;
    const markers = new Map(); // nodeId -> marker
    let bounds = null;

    function ensureLeafletReady() {
        return typeof window.L !== "undefined" && typeof window.L.map === "function";
    }

    function initMap() {
        const mapDiv = document.getElementById("map");
        if (!mapDiv) {
            console.warn("[map] #map div not found");
            return;
        }
        if (!ensureLeafletReady()) {
            console.warn("[map] Leaflet (L) not loaded yet. Check script order.");
            return;
        }
        if (map) return; // prevent double init

        // Leaflet accepts element id string or the element itself 
        map = L.map("map").setView([0, 0], 2);

        L.tileLayer("https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png", {
            maxZoom: 19,
            attribution: "&copy; OpenStreetMap contributors"
        }).addTo(map);

        // If the container size changes after init, Leaflet needs invalidateSize 
        setTimeout(() => {
            try { map.invalidateSize(true); } catch { }
        }, 50);

        bounds = L.latLngBounds([]);
        console.log("[map] initialized");
    }

    function upsertMarker(evt) {
        if (!map) return;

        const lat = Number(evt.latitude ?? evt.lat);
        const lon = Number(evt.longitude ?? evt.lon);
        if (!Number.isFinite(lat) || !Number.isFinite(lon)) return;

        const ts = Number(evt.tsMs);
        const dtStr = Number.isFinite(ts) ? new Date(ts).toLocaleString() : "Unknown time";

        const popup =
            `<b>${evt.nodeId}</b><br/>` +
            `${evt.motion ? "Motion" : "Idle"}<br/>` +
            `Last: ${dtStr}` +
            (evt.sats != null ? `<br/>Sats: ${evt.sats}` : "") +
            (evt.hdop != null ? `<br/>HDOP: ${evt.hdop}` : "");

        if (!markers.has(evt.nodeId)) {
            const m = L.marker([lat, lon]).addTo(map).bindPopup(popup);
            markers.set(evt.nodeId, m);

            // keep view sensible as nodes appear
            bounds.extend([lat, lon]);
            map.fitBounds(bounds, { padding: [20, 20], maxZoom: 16 });
        } else {
            const m = markers.get(evt.nodeId);
            m.setLatLng([lat, lon]);
            m.setPopupContent(popup);
        }
    }

    // ----- RADAR UI -----
    function upsertCard(evt) {
        if (!grid) return;

        if (!cards.has(evt.nodeId)) {
            const col = document.createElement("div");
            col.className = "col-12 col-md-6 col-lg-3";
            col.innerHTML = `
        <div class="card radar-card shadow-sm rounded-4">
          <div class="card-body">
            <div class="d-flex justify-content-between align-items-center">
              <div class="fw-semibold" data-name></div>
              <span class="badge" data-badge></span>
            </div>
            <div class="text-muted small mt-2" data-time></div>
          </div>
        </div>
      `;
            grid.appendChild(col);

            cards.set(evt.nodeId, {
                name: col.querySelector("[data-name]"),
                badge: col.querySelector("[data-badge]"),
                time: col.querySelector("[data-time]")
            });
        }

        const c = cards.get(evt.nodeId);
        c.name.textContent = evt.nodeId;

        const motion = !!evt.motion;
        c.badge.textContent = motion ? "Motion" : "Idle";
        c.badge.className = "badge " + (motion ? "text-bg-success" : "text-bg-secondary");

        const ts = Number(evt.tsMs);
        c.time.textContent = Number.isFinite(ts)
            ? `Last: ${new Date(ts).toLocaleString()}`
            : "Last: Unknown";
    }

    function addLog(evt) {
        if (!log) return;

        const ts = Number(evt.tsMs);
        const dt = Number.isFinite(ts) ? new Date(ts).toLocaleTimeString() : "--:--:--";

        const line = document.createElement("div");
        line.textContent = `[${dt}] ${evt.nodeId}: ${evt.motion ? "Motion" : "Idle"}`;
        log.prepend(line);

        while (log.childElementCount > MAX_LOG_ENTRIES) {
            log.removeChild(log.lastElementChild);
        }
    }

    clearBtn?.addEventListener("click", () => (log.innerHTML = ""));

    // ----- STARTUP -----
    document.addEventListener("DOMContentLoaded", () => {
        initMap();

        if (typeof signalR === "undefined") {
            console.error("[signalr] signalR not loaded. Check script order.");
            return;
        }

        const connection = new signalR.HubConnectionBuilder()
            .withUrl("/radarHub")
            .withAutomaticReconnect()
            .build();

        connection.on("radarEvent", (evt) => {
            try {
                upsertCard(evt);
                addLog(evt);
                upsertMarker(evt);
            } catch (e) {
                console.error("[radarEvent] handler error", e, evt);
            }
        });

        connection.start()
            .then(() => console.log("[signalr] connected"))
            .catch(err => console.error("[signalr] start failed", err));
    });
})();
