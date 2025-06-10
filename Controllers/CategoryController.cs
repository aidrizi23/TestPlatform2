using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TestPlatform2.Data;
using TestPlatform2.Models;
using TestPlatform2.Repository;

namespace TestPlatform2.Controllers;

[Authorize]
public class CategoryController : Controller
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly ITagRepository _tagRepository;
    private readonly UserManager<User> _userManager;

    public CategoryController(
        ICategoryRepository categoryRepository,
        ITagRepository tagRepository,
        UserManager<User> userManager)
    {
        _categoryRepository = categoryRepository;
        _tagRepository = tagRepository;
        _userManager = userManager;
    }

    // GET: /Category/Index
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
            return RedirectToAction("Login", "Account");

        var categories = await _categoryRepository.GetCategoriesWithTestCountAsync(user.Id);
        var tags = await _tagRepository.GetTagsWithTestCountAsync(user.Id);

        var viewModel = new CategoryManagementViewModel
        {
            Categories = categories.Select(c => new CategoryViewModel
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                Color = c.Color,
                Icon = c.Icon,
                TestCount = c.Tests.Count,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt
            }),
            Tags = tags.Select(t => new TagViewModel
            {
                Id = t.Id,
                Name = t.Name,
                Color = t.Color,
                TestCount = t.Tests.Count,
                CreatedAt = t.CreatedAt
            })
        };

        return View(viewModel);
    }

    // POST: /Category/CreateCategory
    [HttpPost]
    public async Task<IActionResult> CreateCategory([FromBody] CreateCategoryViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState
                .Where(x => x.Value.Errors.Count > 0)
                .Select(x => new { Field = x.Key, Message = x.Value.Errors.First().ErrorMessage })
                .ToArray();
            
            return Json(new { success = false, errors = errors });
        }

        var user = await _userManager.GetUserAsync(User);
        if (user is null)
            return Json(new { success = false, message = "User not authenticated" });

        try
        {
            var category = new TestCategory
            {
                Name = model.Name,
                Description = model.Description,
                Color = model.Color,
                Icon = model.Icon,
                UserId = user.Id
            };

            await _categoryRepository.CreateCategoryAsync(category);

            return Json(new
            {
                success = true,
                message = "Category created successfully!",
                category = new
                {
                    id = category.Id,
                    name = category.Name,
                    description = category.Description,
                    color = category.Color,
                    icon = category.Icon,
                    testCount = 0,
                    createdAt = category.CreatedAt
                }
            });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Error creating category" });
        }
    }

    // POST: /Category/UpdateCategory
    [HttpPost]
    public async Task<IActionResult> UpdateCategory([FromBody] EditCategoryViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState
                .Where(x => x.Value.Errors.Count > 0)
                .Select(x => new { Field = x.Key, Message = x.Value.Errors.First().ErrorMessage })
                .ToArray();
            
            return Json(new { success = false, errors = errors });
        }

        var user = await _userManager.GetUserAsync(User);
        if (user is null)
            return Json(new { success = false, message = "User not authenticated" });

        try
        {
            var category = await _categoryRepository.GetCategoryByIdAsync(model.Id);
            if (category is null)
                return Json(new { success = false, message = "Category not found" });

            if (category.UserId != user.Id)
                return Json(new { success = false, message = "Unauthorized access" });

            category.Name = model.Name;
            category.Description = model.Description;
            category.Color = model.Color;
            category.Icon = model.Icon;

            await _categoryRepository.UpdateCategoryAsync(category);

            return Json(new
            {
                success = true,
                message = "Category updated successfully!",
                category = new
                {
                    id = category.Id,
                    name = category.Name,
                    description = category.Description,
                    color = category.Color,
                    icon = category.Icon
                }
            });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Error updating category" });
        }
    }

    // POST: /Category/DeleteCategory
    [HttpPost]
    public async Task<IActionResult> DeleteCategory([FromBody] DeleteRequest request)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
            return Json(new { success = false, message = "User not authenticated" });

        try
        {
            var category = await _categoryRepository.GetCategoryByIdAsync(request.Id);
            if (category is null)
                return Json(new { success = false, message = "Category not found" });

            if (category.UserId != user.Id)
                return Json(new { success = false, message = "Unauthorized access" });

            await _categoryRepository.DeleteCategoryAsync(request.Id);

            return Json(new
            {
                success = true,
                message = "Category deleted successfully!"
            });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Error deleting category" });
        }
    }

    // POST: /Category/CreateTag
    [HttpPost]
    public async Task<IActionResult> CreateTag([FromBody] CreateTagViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState
                .Where(x => x.Value.Errors.Count > 0)
                .Select(x => new { Field = x.Key, Message = x.Value.Errors.First().ErrorMessage })
                .ToArray();
            
            return Json(new { success = false, errors = errors });
        }

        var user = await _userManager.GetUserAsync(User);
        if (user is null)
            return Json(new { success = false, message = "User not authenticated" });

        try
        {
            var tag = new TestTag
            {
                Name = model.Name,
                Color = model.Color,
                UserId = user.Id
            };

            await _tagRepository.CreateTagAsync(tag);

            return Json(new
            {
                success = true,
                message = "Tag created successfully!",
                tag = new
                {
                    id = tag.Id,
                    name = tag.Name,
                    color = tag.Color,
                    testCount = 0,
                    createdAt = tag.CreatedAt
                }
            });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Error creating tag" });
        }
    }

    // POST: /Category/UpdateTag
    [HttpPost]
    public async Task<IActionResult> UpdateTag([FromBody] EditTagViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState
                .Where(x => x.Value.Errors.Count > 0)
                .Select(x => new { Field = x.Key, Message = x.Value.Errors.First().ErrorMessage })
                .ToArray();
            
            return Json(new { success = false, errors = errors });
        }

        var user = await _userManager.GetUserAsync(User);
        if (user is null)
            return Json(new { success = false, message = "User not authenticated" });

        try
        {
            var tag = await _tagRepository.GetTagByIdAsync(model.Id);
            if (tag is null)
                return Json(new { success = false, message = "Tag not found" });

            if (tag.UserId != user.Id)
                return Json(new { success = false, message = "Unauthorized access" });

            tag.Name = model.Name;
            tag.Color = model.Color;

            await _tagRepository.UpdateTagAsync(tag);

            return Json(new
            {
                success = true,
                message = "Tag updated successfully!",
                tag = new
                {
                    id = tag.Id,
                    name = tag.Name,
                    color = tag.Color
                }
            });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Error updating tag" });
        }
    }

    // POST: /Category/DeleteTag
    [HttpPost]
    public async Task<IActionResult> DeleteTag([FromBody] DeleteRequest request)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
            return Json(new { success = false, message = "User not authenticated" });

        try
        {
            var tag = await _tagRepository.GetTagByIdAsync(request.Id);
            if (tag is null)
                return Json(new { success = false, message = "Tag not found" });

            if (tag.UserId != user.Id)
                return Json(new { success = false, message = "Unauthorized access" });

            await _tagRepository.DeleteTagAsync(request.Id);

            return Json(new
            {
                success = true,
                message = "Tag deleted successfully!"
            });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Error deleting tag" });
        }
    }

    // GET: /Category/GetUserCategories
    [HttpGet]
    public async Task<IActionResult> GetUserCategories()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
            return Json(new { success = false, message = "User not authenticated" });

        try
        {
            var categories = await _categoryRepository.GetCategoriesByUserIdAsync(user.Id);
            var categoryList = categories.Select(c => new
            {
                id = c.Id,
                name = c.Name,
                color = c.Color,
                icon = c.Icon
            });

            return Json(new { success = true, data = categoryList });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Error retrieving categories" });
        }
    }

    // GET: /Category/GetUserTags
    [HttpGet]
    public async Task<IActionResult> GetUserTags()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
            return Json(new { success = false, message = "User not authenticated" });

        try
        {
            var tags = await _tagRepository.GetTagsByUserIdAsync(user.Id);
            var tagList = tags.Select(t => new
            {
                id = t.Id,
                name = t.Name,
                color = t.Color
            });

            return Json(new { success = true, data = tagList });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Error retrieving tags" });
        }
    }

    public class DeleteRequest
    {
        public string Id { get; set; }
    }
}