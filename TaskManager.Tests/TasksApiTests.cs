using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using TaskManager.Api.Models;

namespace TaskManager.Tests;

public class TasksApiTests : IDisposable
{
    // Isti JSON kao API: camelCase imena i enum kao tekst
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    // Svaki test dobija svoju aplikaciju i svježu bazu
    private readonly TaskManagerApiFactory _factory = new();
    private readonly HttpClient _client;

    public TasksApiTests()
    {
        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    private async Task<TaskItem> CreateTaskAsync(string title)
    {
        var response = await _client.PostAsJsonAsync("/api/tasks", new TaskItem { Title = title }, JsonOptions);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TaskItem>(JsonOptions))!;
    }

    [Fact]
    public async Task GetAll_WhenEmpty_Returns200AndEmptyList()
    {
        var response = await _client.GetAsync("/api/tasks");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var tasks = await response.Content.ReadFromJsonAsync<List<TaskItem>>(JsonOptions);
        Assert.Empty(tasks!);
    }

    [Fact]
    public async Task Post_ValidTask_Returns201WithLocationAndDefaults()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/tasks",
            new TaskItem { Title = "Napisati API testove", Description = "Integracijski test" },
            JsonOptions);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        var created = await response.Content.ReadFromJsonAsync<TaskItem>(JsonOptions);
        Assert.True(created!.Id > 0);
        Assert.Equal(TaskItemStatus.ToDo, created.Status);
        Assert.EndsWith($"/{created.Id}", response.Headers.Location!.ToString());
    }

    [Fact]
    public async Task Post_Response_ReturnsStatusAsText()
    {
        var response = await _client.PostAsJsonAsync("/api/tasks", new TaskItem { Title = "Provjera statusa" }, JsonOptions);

        var rawJson = await response.Content.ReadAsStringAsync();

        // Regresijski test: status mora biti "ToDo", a ne broj 0
        Assert.Contains("\"status\":\"ToDo\"", rawJson);
    }

    [Fact]
    public async Task Post_EmptyTitle_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/tasks", new TaskItem { Title = "   " }, JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetById_ExistingTask_ReturnsSameTask()
    {
        var created = await CreateTaskAsync("Dohvati me");

        var response = await _client.GetAsync($"/api/tasks/{created.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var task = await response.Content.ReadFromJsonAsync<TaskItem>(JsonOptions);
        Assert.Equal("Dohvati me", task!.Title);
    }

    [Fact]
    public async Task GetById_NonExistingTask_Returns404()
    {
        var response = await _client.GetAsync("/api/tasks/999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Put_ExistingTask_Returns204AndChangesArePersisted()
    {
        var created = await CreateTaskAsync("Stari naslov");

        var response = await _client.PutAsJsonAsync(
            $"/api/tasks/{created.Id}",
            new TaskItem { Title = "Novi naslov", Status = TaskItemStatus.InProgress },
            JsonOptions);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var task = await _client.GetFromJsonAsync<TaskItem>($"/api/tasks/{created.Id}", JsonOptions);
        Assert.Equal("Novi naslov", task!.Title);
        Assert.Equal(TaskItemStatus.InProgress, task.Status);
    }

    [Fact]
    public async Task Delete_ExistingTask_Returns204AndTaskIsGone()
    {
        var created = await CreateTaskAsync("Za brisanje");

        var deleteResponse = await _client.DeleteAsync($"/api/tasks/{created.Id}");
        var getResponse = await _client.GetAsync($"/api/tasks/{created.Id}");

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task Delete_NonExistingTask_Returns404()
    {
        var response = await _client.DeleteAsync("/api/tasks/999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

        private async Task<HttpResponseMessage> PostRawJsonAsync(string json)
    {
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        return await _client.PostAsync("/api/tasks", content);
    }

    [Fact]
    public async Task Post_TitleLongerThan200_Returns400()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/tasks", new TaskItem { Title = new string('a', 201) }, JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_TitleExactly200_Returns201()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/tasks", new TaskItem { Title = new string('a', 200) }, JsonOptions);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Post_DescriptionLongerThan2000_Returns400()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/tasks",
            new TaskItem { Title = "Dug opis", Description = new string('b', 2001) },
            JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_InvalidStatusText_Returns400()
    {
        var response = await PostRawJsonAsync("{\"title\":\"Test\",\"status\":\"Banana\"}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_InvalidStatusNumber_Returns400()
    {
        var response = await PostRawJsonAsync("{\"title\":\"Test\",\"status\":99}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_MalformedJson_Returns400()
    {
        var response = await PostRawJsonAsync("{\"title\": \"Test\"");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Put_TitleLongerThan200_Returns400()
    {
        var created = await CreateTaskAsync("Kratak naslov");

        var response = await _client.PutAsJsonAsync(
            $"/api/tasks/{created.Id}",
            new TaskItem { Title = new string('a', 201) },
            JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
    
}

