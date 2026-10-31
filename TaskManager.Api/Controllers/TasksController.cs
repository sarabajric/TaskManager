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
        if (string.IsNullOrWhiteSpace(task.Title))
            return BadRequest("Title je obavezan.");

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

        if (string.IsNullOrWhiteSpace(updated.Title))
            return BadRequest("Title je obavezan.");

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