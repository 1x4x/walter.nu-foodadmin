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
                item.ImageIds ??= new();
                item.ImageIds = item.ImageIds.Distinct().Where(id => data.Images.Any(image => image.Id == id)).ToList();
                if (!string.IsNullOrEmpty(item.ImageId)) { if (!data.Images.Any(image => image.Id == item.ImageId)) throw new ArgumentException("Unknown image."); if (!item.ImageIds.Contains(item.ImageId)) item.ImageIds.Add(item.ImageId); }
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
    public async Task<IActionResult> Upload(IFormFile file, string alt = "", string foodId = "")
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
        try { store.Update(data => { data.Images.Add(image); var food = data.Foods.FirstOrDefault(x => x.Id == foodId); if (food is not null) { food.ImageIds ??= new(); food.ImageIds.Add(image.Id); } }); }
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
                    foreach (var food in data.Foods) { if (food.ImageId == id) food.ImageId = ""; food.ImageIds?.Remove(id); }
                    var file = Path.Combine(env.WebRootPath, "uploads", Path.GetFileName(image.FileName));
                    if (System.IO.File.Exists(file)) System.IO.File.Delete(file);
                    break;
            }
        });
        return RedirectToAction(nameof(Index));
    }


    [Authorize(Roles = "Admin"), HttpPost, ValidateAntiForgeryToken]
    public IActionResult Gallery(string foodId, string imageId, string operation)
    {
        store.Update(data =>
        {
            var food = data.Foods.FirstOrDefault(x => x.Id == foodId);
            if (food is null) return;
            food.ImageIds ??= new();
            if (operation == "clear") { food.ImageId = ""; return; }
            if (!data.Images.Any(x => x.Id == imageId)) return;
            if (operation == "attach" && !food.ImageIds.Contains(imageId)) food.ImageIds.Add(imageId);
            if (operation == "detach") { food.ImageIds.Remove(imageId); if (food.ImageId == imageId) food.ImageId = ""; }
            if (operation == "select" && food.ImageIds.Contains(imageId)) food.ImageId = imageId;
        });
        return RedirectToAction(nameof(Index), new { tab = "foods" });
    }

    [Authorize(Roles = "Admin"), HttpPost, ValidateAntiForgeryToken]
    public IActionResult BulkFood(string operation, decimal? price, decimal? percent, string? badge, string? categoryId, bool? available)
    {
        store.Update(data =>
        {
            var foods = data.Foods.Where(x => x.IsRealFood && (string.IsNullOrEmpty(categoryId) || x.CategoryId == categoryId));
            foreach (var food in foods)
            {
                switch (operation)
                {
                    case "set-price" when price is >= 0: food.Price = price.Value; break;
                    case "adjust-percent" when percent is >= -100 and <= 1000:
                        food.Price = Math.Max(0, decimal.Round(food.Price * (1 + percent.Value / 100), 2)); break;
                    case "badge": food.Badge = (badge ?? "")[..Math.Min((badge ?? "").Length, 50)]; break;
                    case "availability" when available.HasValue: food.Available = available.Value; break;
                }
            }
        });
        return RedirectToAction(nameof(Index), new { tab = "foods" });
    }

    [Authorize(Roles = "Admin"), HttpPost, ValidateAntiForgeryToken]
    public IActionResult ImportChrilles()
    {
        var seed = MenuStore.LoadChrillesSeed(env);
        store.Update(data =>
        {
            foreach (var cat in seed.Categories)
                if (!data.Categories.Any(x => x.Id == cat.Id)) data.Categories.Add(cat);
            foreach (var food in seed.Foods)
                if (!data.Foods.Any(x => x.Id == food.Id)) data.Foods.Add(food);
            data.Settings.Name = "Chrilles Restaurang";
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