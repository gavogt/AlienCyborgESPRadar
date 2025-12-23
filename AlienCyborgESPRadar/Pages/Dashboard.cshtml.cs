using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AlienCyborgESPRadar.Pages
{
    [Authorize]
    public class DashboardModel : PageModel
    {
        private readonly RadarAnalysisOrchestrator _orchestrator;

        public DashboardModel(RadarAnalysisOrchestrator orchestrator)
        {
            _orchestrator = orchestrator;
        }

        public string Summary { get; private set; } = "";
        public string AnomaliesJson { get; private set; } = "";
        public string Actions { get; private set; } = "";

        public async Task OnGetAsync()
        {
            var rawLogs = "node=7 motion=1 time=2025-12-23T";
            var results = await _orchestrator.AnalyzeAsync(rawLogs, HttpContext.RequestAborted);

            Summary = results.Summary;
            AnomaliesJson = results.AnomaliesJson;
            Actions = results.Actions;
        }
    
    }
}
