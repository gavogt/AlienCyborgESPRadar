using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AlienCyborgESPRadar.Pages
{
    [Authorize]
    public class DashboardModel : PageModel
    {
        private readonly RadarDbContext _radarDb;
        private readonly RadarAnalysisOrchestrator _orchestrator;

        private ILogger<DashboardModel>? _logger;

        public DashboardModel(RadarDbContext radarDb, RadarAnalysisOrchestrator orchestrator, ILogger<DashboardModel> logger)
        {
            _radarDb = radarDb;
            _orchestrator = orchestrator;
            _logger = logger;
        }

        public List<RadarLog> RecentLogs { get; private set; } = new();

        public string Summary { get; private set; } = "Loading analysis...";
        public string AnomaliesJson { get; private set; } = "";
        public string Actions { get; private set; } = "";

        public async Task OnGetAsync()
        {
            RecentLogs = await _radarDb.RadarLogs
                .AsNoTracking()
                .OrderByDescending(r => r.TimestampUtc)
                .Take(40)
                .ToListAsync();

        }

        public async Task<IActionResult> OnGetAnalyzeAsync()
        {
            var logs = await _radarDb.RadarLogs
                .AsNoTracking()
                .OrderByDescending(r => r.TimestampUtc)
                .Take(40)
                .ToListAsync();

            _logger?.LogInformation("Analyze: logs count={count}", logs.Count);

            if (logs.Count == 0)
            {
                return new JsonResult(new
                {
                    Summary = "No radar logs in database yet.",
                    AnomaliesJson = "[]",
                    Actions = "Start ingest/persist pipeline so events get stored, then re-run analysis."
                });
            }

            var raw = string.Join("\n", logs
                .OrderBy(r => r.TimestampUtc)
                .Select(r => $"{r.TimestampUtc:O} node={r.NodeId} motion={(r.Motion ? 1 : 0)} tsMs={r.TsMs}"));

            _logger?.LogInformation("Analyze: rawLogs length={len}", raw.Length);

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(HttpContext.RequestAborted);
            cts.CancelAfter(TimeSpan.FromSeconds(400));

            var results = await _orchestrator.AnalyzeAsync(raw, cts.Token);

            return new JsonResult(new
            {
                results.Summary,
                results.AnomaliesJson,
                results.Actions
            });
        }


    }
}
