using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TaskManager.Api.Controllers;
using TaskManager.Api.Data;
using TaskManager.Api.Models;

namespace TaskManager.Tests;

public class TasksControllerTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _context;
    private readonly TasksController _controller;

    // xUnit za SVAKI test pravi novu instancu klase, pa svaki test dobije svježu praznu bazu
    public TasksControllerTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new AppDbContext(options);
        _context.Database.EnsureCreated();
        _controller = new TasksController(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task GetAll_WhenNoTasks_ReturnsEmptyList()
    {
        var result = await _controller.GetAll();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var tasks = Assert.IsAssignableFrom<IEnumerable<TaskItem>>(ok.Value);
        Assert.Empty(tasks);
    }

    [Fact]
    public async Task Create_WithValidTitle_ReturnsCreatedAndSavesTask()
    {
        var newTask = new TaskItem { Title = "Naučiti xUnit", Description = "Prvi test" };

        var result = await _controller.Create(newTask);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var saved = Assert.IsType<TaskItem>(created.Value);
        Assert.True(saved.Id > 0);
        Assert.Equal(TaskItemStatus.ToDo, saved.Status);
        Assert.Equal(1, await _context.Tasks.CountAsync());
    }

    [Fact]
    public async Task Create_WithEmptyTitle_ReturnsBadRequest()
    {
        var newTask = new TaskItem { Title = "   " };

        var result = await _controller.Create(newTask);

        Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal(0, await _context.Tasks.CountAsync());
    }

    [Fact]
    public async Task GetById_WhenTaskDoesNotExist_ReturnsNotFound()
    {
        var result = await _controller.GetById(999);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task Update_ExistingTask_ChangesFieldsAndReturnsNoContent()
    {
        var task = new TaskItem { Title = "Stari naslov" };
        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();

        var updated = new TaskItem { Title = "Novi naslov", Status = TaskItemStatus.Done };
        var result = await _controller.Update(task.Id, updated);

        Assert.IsType<NoContentResult>(result);
        var fromDb = await _context.Tasks.AsNoTracking().FirstAsync(t => t.Id == task.Id);
        Assert.Equal("Novi naslov", fromDb.Title);
        Assert.Equal(TaskItemStatus.Done, fromDb.Status);
    }

    [Fact]
    public async Task Update_WhenTaskDoesNotExist_ReturnsNotFound()
    {
        var result = await _controller.Update(999, new TaskItem { Title = "Bilo šta" });

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Delete_ExistingTask_RemovesItAndReturnsNoContent()
    {
        var task = new TaskItem { Title = "Za brisanje" };
        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();

        var result = await _controller.Delete(task.Id);

        Assert.IsType<NoContentResult>(result);
        Assert.Equal(0, await _context.Tasks.CountAsync());
    }

    [Fact]
    public async Task Delete_WhenTaskDoesNotExist_ReturnsNotFound()
    {
        var result = await _controller.Delete(999);

        Assert.IsType<NotFoundResult>(result);
    }
}