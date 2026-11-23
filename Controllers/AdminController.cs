using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantVision.Models;
using RestaurantVision.Services;

namespace RestaurantVision.Controllers;

public class AdminController(MenuStore store, IWebHostEnvironment env, IConfiguration configuration) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(string? superadmin)
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            var secret = configuration["Admin:BootstrapKey"];
            if (string.IsNullOrWhiteSpace(secret) || string.IsNullOrWhiteSpace(superadmin)) return NotFound();
            if (!CryptographicOperations.FixedTimeEquals(SHA256.HashData(Encoding.UTF8.GetBytes(secret)), SHA256.HashData(Encoding.UTF8.GetBytes(superadmin))))
                return NotFound();

            var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "Restaurant Admin"), new Claim(ClaimTypes.Role, "Admin") }, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
            return RedirectToAction(nameof(Index)); // remove secret from the address bar
        }

        return View(store.Read());
    }

    [Authorize(Roles = "Admin"), HttpPost, ValidateAntiForgeryToken]
    public IActionResult Food(FoodItem item)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        if (string.IsNullOrWhiteSpace(item.CategoryId)) return BadRequest("Choose a category.");
        try
        {
            store.Update(data =>
            {
                if (!data.Categories.Any(category => category.Id == item.CategoryId)) throw new ArgumentException("Unknown category");
                if (!string.IsNullOrEmpty(item.ImageId) && !data.Images.Any(image => image.Id == item.ImageId)) throw new ArgumentException("Unknown image");
                data.Foods.RemoveAll(food => food.Id == item.Id);
                data.Foods.Add(item);
            });
        }
        catch (ArgumentException exception) { return BadRequest(exception.Message); }
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin"), HttpPost, ValidateAntiForgeryToken]
    public IActionResult Category(FoodCategory item)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        store.Update(data => { data.Categories.RemoveAll(category => category.Id == item.Id); data.Categories.Add(item); });
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin"), HttpPost, ValidateAntiForgeryToken]
    public IActionResult Settings(RestaurantSettings item)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        item.Theme = item.Theme is "noir" or "ember" or "neon" ? item.Theme : "noir";
        item.Accent = System.Text.RegularExpressions.Regex.IsMatch(item.Accent ?? "", "^#[0-9a-fA-F]{6}$") ? item.Accent : "#f4b860";
        item.SlideSeconds = Math.Clamp(item.SlideSeconds, 3, 120);
        store.Update(data => data.Settings = item);
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin"), HttpPost, ValidateAntiForgeryToken]
    [RequestSizeLimit(9 * 1024 * 1024)]
    public async Task<IActionResult> Upload(IFormFile file, string alt = "")
    {
        if (file is null || file.Length <= 0 || file.Length > 8 * 1024 * 1024) return BadRequest("Image must be between 1 byte and 8 MB.");
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext is not (".jpg" or ".jpeg" or ".png" or ".webp")) return BadRequest("Only JPEG, PNG and WebP are supported.");
        await using var stream = file.OpenReadStream();
        var header = new byte[12];
        var read = await stream.ReadAsync(header);
        stream.Position = 0;
        var jpeg = read >= 3 && header[0] == 255 && header[1] == 216 && header[2] == 255;
        var png = read >= 8 && header.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 });
        var webp = read >= 12 && Encoding.ASCII.GetString(header, 0, 4) == "RIFF" && Encoding.ASCII.GetString(header, 8, 4) == "WEBP";
        if (!(ext is ".jpg" or ".jpeg" && jpeg || ext == ".png" && png || ext == ".webp" && webp))
            return BadRequest("File content does not match the image extension.");

        var image = new FoodImage { FileName = Guid.NewGuid().ToString("N") + ext, Alt = alt.Length > 240 ? alt[..240] : alt };
        var directory = Path.Combine(env.WebRootPath, "uploads");
        Directory.CreateDirectory(directory);
        var fullPath = Path.Combine(directory, image.FileName);
        await using (var destination = System.IO.File.Create(fullPath)) await stream.CopyToAsync(destination);
        try { store.Update(data => data.Images.Add(image)); }
        catch { System.IO.File.Delete(fullPath); throw; }
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin"), HttpPost, ValidateAntiForgeryToken]
    public IActionResult Image(FoodImage edited)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        store.Update(data =>
        {
            var image = data.Images.FirstOrDefault(x => x.Id == edited.Id);
            if (image is not null) image.Alt = edited.Alt;
        });
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin"), HttpPost, ValidateAntiForgeryToken]
    public IActionResult Delete(string type, string id)
    {
        store.Update(data =>
        {
            switch (type)
            {
                case "food":
                    data.Foods.RemoveAll(x => x.Id == id);
                    break;
                case "category":
                    data.Categories.RemoveAll(x => x.Id == id);
                    foreach (var food in data.Foods.Where(x => x.CategoryId == id)) food.CategoryId = "";
                    break;
                case "image":
                    var image = data.Images.FirstOrDefault(x => x.Id == id);
                    if (image is null) break;
                    data.Images.Remove(image);
                    foreach (var food in data.Foods.Where(x => x.ImageId == id)) food.ImageId = "";
                    var file = Path.Combine(env.WebRootPath, "uploads", Path.GetFileName(image.FileName));
                    if (System.IO.File.Exists(file)) System.IO.File.Delete(file);
                    break;
            }
        });
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin"), HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Home");
    }
}