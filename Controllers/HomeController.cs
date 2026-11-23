using Microsoft.AspNetCore.Mvc;
using RestaurantVision.Services;

namespace RestaurantVision.Controllers;
public class HomeController(MenuStore store) : Controller
{
    public IActionResult Index() => View(store.Read());

    [HttpGet]
    public IActionResult Tv(string? theme)
    {
        var model = store.Read();
        if (theme is "noir" or "ember" or "neon") model.Settings.Theme = theme;
        return View("Index", model);
    }

    public IActionResult Error() => Content("An unexpected error occurred.", "text/plain");
}