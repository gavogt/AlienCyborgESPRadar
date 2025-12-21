const grid = document.getElementById("radarGrid");
const log = document.getElementById("log");
const clearBtn = document.getElementById("clearLog");

const cards = new Map(); // nodeId -> elements

function upsertCard(evt) {
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
            time: col.querySelector("[data-time]"),
        });
    }

    const c = cards.get(evt.nodeId);
    c.name.textContent = evt.nodeId;

    const motion = !!evt.motion;
    c.badge.textContent = motion ? "Motion" : "Idle";
    c.badge.className = "badge " + (motion ? "text-bg-success" : "text-bg-secondary");

    const ts = Number(evt.tsMs);         
    const dt = new Date(ts);
    c.time.textContent = `Last: ${dt.toLocaleString()}`;
}

function addLog(evt) {
    const ts = Number(evt.tsMs);         
    const dt = new Date(ts).toLocaleTimeString();
    const line = document.createElement("div");
    line.textContent = `[${dt}] ${evt.nodeId}: ${evt.motion ? "Motion" : "Idle"}`;
    log.prepend(line);
}

clearBtn?.addEventListener("click", () => (log.innerHTML = ""));

const connection = new signalR.HubConnectionBuilder()
    .withUrl("/radarHub")
    .withAutomaticReconnect()
    .build();

// JS client receives hub messages using .on(...)
connection.on("radarEvent", (evt) => {
    upsertCard(evt);
    addLog(evt);
});

connection.start();