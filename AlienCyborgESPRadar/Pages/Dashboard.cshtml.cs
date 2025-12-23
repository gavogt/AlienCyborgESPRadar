using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AlienCyborgESPRadar.Pages
{
    [Authorize]
    public class Dashboard : PageModel
    {
        public void OnGet()
        {
        }
    }
}
