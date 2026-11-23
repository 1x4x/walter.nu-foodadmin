using System.ComponentModel.DataAnnotations;
namespace RestaurantVision.Models;
public class FoodCategory
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    [Required, StringLength(100)] public string Name { get; set; } = "";
    [StringLength(300)] public string Description { get; set; } = "";
    public int SortOrder { get; set; }
    public bool Visible { get; set; } = true;
}
public class FoodItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    [Required, StringLength(120)] public string Name { get; set; } = "";
    [StringLength(500)] public string Description { get; set; } = "";
    [Range(0, 1000000)] public decimal Price { get; set; }
    public string CategoryId { get; set; } = "";
    public string ImageId { get; set; } = ""; // selected image; empty means no image
    public List<string> ImageIds { get; set; } = new(); // image gallery per food
    public bool IsRealFood { get; set; } = true;
    [StringLength(50)] public string Badge { get; set; } = "";
    [StringLength(250)] public string Allergens { get; set; } = "";
    public bool Available { get; set; } = true;
    public bool Featured { get; set; }
    public int SortOrder { get; set; }
}
public class FoodImage
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string FileName { get; set; } = "";
    [StringLength(240)] public string Alt { get; set; } = "";
    public DateTime UploadedUtc { get; set; } = DateTime.UtcNow;
}
public class RestaurantSettings
{
    public string Theme { get; set; } = "noir";
    [Required, StringLength(100)] public string Name { get; set; } = "Chrilles Restaurang";
    [StringLength(120)] public string Subtitle { get; set; } = "THE ART OF GOOD FOOD";
    [StringLength(120)] public string Headline { get; set; } = "Made to crave.";
    [StringLength(280)] public string Description { get; set; } = "Bold flavors. Fresh ingredients. Unforgettable moments.";
    [StringLength(12)] public string Currency { get; set; } = "kr";
    public string Accent { get; set; } = "#f4b860";
    [StringLength(8)] public string LogoText { get; set; } = "N&F";
    public bool ShowPrices { get; set; } = true;
    public int SlideSeconds { get; set; } = 12;
}
public class MenuData
{
    public List<FoodCategory> Categories { get; set; } = new();
    public List<FoodItem> Foods { get; set; } = new();
    public List<FoodImage> Images { get; set; } = new();
    public RestaurantSettings Settings { get; set; } = new();
}