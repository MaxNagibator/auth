using Microsoft.AspNetCore.Mvc;

namespace Auth.Api.Controllers;

public class ErrorController : Controller
{
    [Route("/error")]
    public IActionResult Index()
    {
        return View();
    }
}
