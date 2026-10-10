using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ToDoListApp.Data;
using ToDoListApp.Models;

namespace ToDoListApp.Controllers;

public class TodoItemController : Controller
{
    private const int PageSize = 5;
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;

    public TodoItemController(AppDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    private string UploadsFolder => Path.Combine(_env.ContentRootPath, "Uploads");

    // GET: /TodoItem?searchDate=2026-03-30&page=1
    public async Task<IActionResult> Index(DateTime? searchDate, int page = 1)
    {
        var query = _db.TodoItems.AsQueryable();

        if (searchDate.HasValue)
        {
            var start = searchDate.Value.Date;
            var end = start.AddDays(1);
            query = query.Where(t => t.Deadline >= start && t.Deadline < end);
        }

        var total = await query.CountAsync();
        var totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)PageSize));
        page = Math.Clamp(page, 1, totalPages);

        var items = await query
            .OrderBy(t => t.Deadline)
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();

        ViewBag.Page = page;
        ViewBag.TotalPages = totalPages;
        ViewBag.SearchDate = searchDate?.ToString("yyyy-MM-dd");

        return View(items);
    }

    // GET: /TodoItem/Create
    public IActionResult Create() => View();

    // POST: /TodoItem/Create
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TodoItem item, IFormFile? file)
    {
        if (!ModelState.IsValid) return View(item);

        await SaveFileAsync(item, file);
        _db.Add(item);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    // GET: /TodoItem/Edit/5
    public async Task<IActionResult> Edit(int id)
    {
        var item = await _db.TodoItems.FindAsync(id);
        return item == null ? NotFound() : View(item);
    }

    // POST: /TodoItem/Edit/5
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, TodoItem model, IFormFile? file)
    {
        if (id != model.Id) return BadRequest();
        if (!ModelState.IsValid) return View(model);

        var item = await _db.TodoItems.FindAsync(id);
        if (item == null) return NotFound();

        item.Title = model.Title;
        item.Description = model.Description;
        item.Deadline = model.Deadline;

        if (file is { Length: > 0 })
        {
            DeleteFile(item.FilePath);
            await SaveFileAsync(item, file);
        }

        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    // POST: /TodoItem/Delete/5
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _db.TodoItems.FindAsync(id);
        if (item != null)
        {
            DeleteFile(item.FilePath);
            _db.Remove(item);
            await _db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }

    // GET: /TodoItem/Download/5
    public async Task<IActionResult> Download(int id)
    {
        var item = await _db.TodoItems.FindAsync(id);
        if (item?.FilePath == null) return NotFound();

        var fullPath = Path.Combine(UploadsFolder, item.FilePath);
        if (!System.IO.File.Exists(fullPath)) return NotFound();

        var bytes = await System.IO.File.ReadAllBytesAsync(fullPath);
        return File(bytes, "application/octet-stream", item.OriginalFileName ?? item.FilePath);
    }

    private async Task SaveFileAsync(TodoItem item, IFormFile? file)
    {
        if (file == null || file.Length == 0) return;

        Directory.CreateDirectory(UploadsFolder);
        var storedName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
        await using var stream = System.IO.File.Create(Path.Combine(UploadsFolder, storedName));
        await file.CopyToAsync(stream);

        item.FilePath = storedName;
        item.OriginalFileName = Path.GetFileName(file.FileName);
    }

    private void DeleteFile(string? storedName)
    {
        if (string.IsNullOrEmpty(storedName)) return;
        var path = Path.Combine(UploadsFolder, storedName);
        if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
    }
}
