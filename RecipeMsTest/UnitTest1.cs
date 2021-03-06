using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Recipe.Dal.Models;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace RecipeMsTest
{
    [TestClass]
    public class UnitTest1
    {
        public class RecipeTests
        {
            //private readonly ITestOutputHelper output;
            private RecipeContext dc = RecipeContext.RecipeContextFactory();

            //public RecipeTests(ITestOutputHelper output)
            //{
            //    this.output = output;
            //}

            [TestMethod]
            public void Recipe_Load()
            {
                var items = dc.Recipes.Take(5);
                Assert.IsTrue(items.Any());
                foreach (var recipe in items)
                {
                    Console.WriteLine(recipe.Title);
                }
            }

            [TestMethod]
            public void Category_Create()
            {
                var newCategory = new Category { Description = $"Test {DateTime.Now.Ticks}" };
                dc.Categories.Add(newCategory);
                dc.SaveChanges();
                Assert.IsTrue(newCategory.Id > 0);

                // Cleanup
                dc.Categories.Remove(newCategory);
                dc.SaveChanges();
            }

            [TestMethod]
            public async Task Recipe_CanCreateBatchAsync()
            {
                var recipe = new Recipe.Dal.Models.Recipe
                {
                    Title = "Add Test",
                    ServingMeasure = "Bites",
                    ServingQuantity = 42
                };
                dc.Recipes.Add(recipe);
                for (int i = 0; i < 10; i++)
                {
                    recipe.Ingredients.Add(new Ingredient { Description = $"Ing {i}", SortOrder = i, Units = i.ToString(), UnitType = "pn" });
                    recipe.Directions.Add(new Direction { Description = $"Step {i} - Stir", LineNumber = i });
                }
                await dc.SaveChangesAsync();
                Assert.IsTrue(recipe.Id > 0);

                // Cleanup
                dc.Ingredients.RemoveRange(recipe.Ingredients);
                dc.Directions.RemoveRange(recipe.Directions);
                dc.Recipes.Remove(recipe);
                await dc.SaveChangesAsync();
            }
            [TestMethod]
            public async Task StoredProcsAsync()
            {
                var brownies = await dc.SearchRecipeAsync("Brownie");
                Assert.AreNotEqual(0, brownies.Count());
            }
            [TestMethod]
            public async Task StoredProcs_CanExtendAsync()
            {
                // Note, this version lies because stored procs aren't extendable
                // so the additional query portions are done client side.
                var brownies = await dc.SearchRecipeOrderedAsync("Brownie");
                Assert.IsTrue(brownies.Any());
            }

            [TestMethod]
            public void Recipe_EagerLoading()
            {
                var brownies = from r in dc.Recipes
                                    .Include(rec => rec.RecipeCategories).ThenInclude(rc => rc.Category)
                                    .Include(rec => rec.Ingredients)
                                    .Include(rec => rec.Directions)
                               where r.Title.Contains("brownie")
                               select r;

                foreach (var recipe in brownies.Take(5).ToList())
                {
                    Console.WriteLine(recipe.Title);
                    Console.WriteLine($"    Category: " + recipe.RecipeCategories.FirstOrDefault()?.Category?.Description);

                    foreach (var ingredient in recipe.Ingredients.OrderBy(i => i.SortOrder))
                    {
                        Console.WriteLine($"{ingredient.Units} {ingredient.UnitType}: {ingredient.Description}");
                    }

                    foreach (var directionLine in recipe.Directions.OrderBy(d => d.LineNumber))
                    {
                        Console.WriteLine(directionLine.Description);
                    }
                }
            }

            [TestMethod]
            public void Recipe_Projections()
            {
                var brownies = from r in dc.Recipes
                               where r.Title.Contains("Brownie")
                               select new
                               {
                                   r.Title,
                                   Categories = r.RecipeCategories.Select(rc => rc.Category.Description),
                                   Ingredients = r.Ingredients.OrderBy(i => i.SortOrder),
                                   Directions = r.Directions.OrderBy(d => d.LineNumber).Select(d => d.Description)
                               };

                foreach (var recipe in brownies.Take(5).ToList())
                {
                    Console.WriteLine(recipe.Title);
                    foreach (var category in recipe.Categories)
                    {
                        Console.WriteLine($"    Category: " + category);
                    }

                    foreach (var ingredient in recipe.Ingredients)
                    {
                        Console.WriteLine($"{ingredient.Units} {ingredient.UnitType}: {ingredient.Description}");
                    }

                    foreach (var directionLine in recipe.Directions)
                    {
                        Console.WriteLine(directionLine);
                    }
                }
            }

            //[TestMethod]
            public void Recipe_WithoutNavigationProperties()
            {
                var brownies = from r in dc.Recipes
                               where r.Title.Contains("Brownie")
                               select new
                               {
                                   r.Title,
                                   Categories = dc.RecipeCategory.Where(rc => rc.RecipeId == r.Id).Select(rc => rc.Category.Description),
                                   Ingredients = dc.Ingredients.Where(i => i.RecipeId == r.Id).OrderBy(i => i.SortOrder),
                                   Directions = dc.Directions.Where(d => d.RecipeId == r.Id).OrderBy(d => d.LineNumber).Select(d => d.Description)
                               };

                foreach (var recipe in brownies.Take(5).ToList())
                {
                    Console.WriteLine(recipe.Title);
                    foreach (var category in recipe.Categories)
                    {
                        Console.WriteLine($"    Category: " + category);
                    }

                    foreach (var ingredient in recipe.Ingredients)
                    {
                        Console.WriteLine($"{ingredient.Units} {ingredient.UnitType}: {ingredient.Description}");
                    }

                    foreach (var directionLine in recipe.Directions)
                    {
                        Console.WriteLine(directionLine);
                    }
                }
            }
        }
    }
}
