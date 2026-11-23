using System.Text.Json;
using RestaurantVision.Models;

namespace RestaurantVision.Services;
public sealed class MenuStore
{
    private readonly string path;
    private readonly object gate = new();
    private readonly JsonSerializerOptions options = new() { WriteIndented = true, PropertyNameCaseInsensitive = true };

    public MenuStore(IWebHostEnvironment env)
    {
        var directory = Path.Combine(env.ContentRootPath, "Data");
        Directory.CreateDirectory(directory);
        path = Path.Combine(directory, "menu.json");
        if (!File.Exists(path)) Save(CreateDemo());
    }

    public MenuData Read()
    {
        lock (gate)
            return JsonSerializer.Deserialize<MenuData>(File.ReadAllText(path), options) ?? new();
    }

    public void Update(Action<MenuData> change)
    {
        lock (gate)
        {
            var data = Read();
            change(data);
            Save(data);
        }
    }

    private void Save(MenuData data)
    {
        var temporary = path + ".tmp";
        File.WriteAllText(temporary, JsonSerializer.Serialize(data, options));
        File.Move(temporary, path, true);
    }

    private static MenuData CreateDemo()
    {
        var data = new MenuData();
        var burgers = new FoodCategory { Name = "Signature Burgers", Description = "Fire-grilled perfection", SortOrder = 1 };
        var pizza = new FoodCategory { Name = "Stone-Baked Pizza", Description = "Straight from the oven", SortOrder = 2 };
        var sides = new FoodCategory { Name = "Sides & More", Description = "The perfect companions", SortOrder = 3 };
        data.Categories.AddRange(new[] { burgers, pizza, sides });
        data.Foods.AddRange(new[]
        {
            new FoodItem { Name = "The Midnight Smash", Description = "Double smashed beef, aged cheddar, secret sauce, brioche bun", Price = 169, CategoryId = burgers.Id, Badge = "BEST SELLER", Featured = true, SortOrder = 1 },
            new FoodItem { Name = "Truffle Royale", Description = "Juicy beef, truffle aioli, caramelized onion, parmesan", Price = 189, CategoryId = burgers.Id, Badge = "CHEF'S PICK", SortOrder = 2 },
            new FoodItem { Name = "Hot Honey Pepperoni", Description = "Mozzarella, spicy pepperoni, basil and hot honey drizzle", Price = 179, CategoryId = pizza.Id, Badge = "HOT", Featured = true, SortOrder = 1 },
            new FoodItem { Name = "Burrata Margherita", Description = "San Marzano tomato, fresh burrata, basil, olive oil", Price = 165, CategoryId = pizza.Id, SortOrder = 2 },
            new FoodItem { Name = "Parmesan Fries", Description = "Golden fries, parmesan snow, herbs, garlic dip", Price = 69, CategoryId = sides.Id, SortOrder = 1 },
            new FoodItem { Name = "Crispy Chicken Bites", Description = "Buttermilk fried chicken, spicy house mayo", Price = 99, CategoryId = sides.Id, SortOrder = 2 }
        });
        return data;
    }
}