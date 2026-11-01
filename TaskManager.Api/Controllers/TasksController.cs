using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskManager.Api.Data;
using TaskManager.Api.Models;

namespace TaskManager.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TasksController : ControllerBase
{
    private readonly AppDbContext _context;

    // AppDbContext dobijamo preko dependency injectiona (registrovan u Program.cs)
    public TasksController(AppDbContext context)
    {
        _context = context;
    }

    private const int MaxTitleLength = 200;
    private const int MaxDescriptionLength = 2000;

    // Vraća poruku o grešci, ili null ako je zadatak ispravan
    private static string? Validate(TaskItem task)
    {
        if (string.IsNullOrWhiteSpace(task.Title))
            return "Title je obavezan.";

        if (task.Title.Length > MaxTitleLength)
            return $"Title može imati najviše {MaxTitleLength} znakova.";

        if (task.Description is { Length: > MaxDescriptionLength })
            return $"Description može imati najviše {MaxDescriptionLength} znakova.";

        if (!Enum.IsDefined(task.Status))
            return "Status nije ispravan.";

        return null;
    }

    // GET api/tasks
    [HttpGet]
    public async Task<ActionResult<IEnumerable<TaskItem>>> GetAll()
    {
        var tasks = await _context.Tasks.ToListAsync();
        return Ok(tasks);
    }

    // GET api/tasks/5
    [HttpGet("{id}")]
    public async Task<ActionResult<TaskItem>> GetById(int id)
    {
        var task = await _context.Tasks.FindAsync(id);
        if (task == null)
            return NotFound();

        return Ok(task);
    }

    // POST api/tasks
    [HttpPost]
    public async Task<ActionResult<TaskItem>> Create(TaskItem task)
    {
                var error = Validate(task);
        if (error != null)
            return BadRequest(error);

        task.Id = 0;
        task.CreatedAt = DateTime.UtcNow;

        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = task.Id }, task);
    }

    // PUT api/tasks/5
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, TaskItem updated)
    {
        var task = await _context.Tasks.FindAsync(id);
        if (task == null)
            return NotFound();

                var error = Validate(updated);
        if (error != null)
            return BadRequest(error);

        task.Title = updated.Title;
        task.Description = updated.Description;
        task.Status = updated.Status;
        task.DueDate = updated.DueDate;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    // DELETE api/tasks/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var task = await _context.Tasks.FindAsync(id);
        if (task == null)
            return NotFound();

        _context.Tasks.Remove(task);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}