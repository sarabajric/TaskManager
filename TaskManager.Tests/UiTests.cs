using System.Text.RegularExpressions;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace TaskManager.Tests;

// UI testovi: Playwright otvara pravu stranicu u Chromiumu i klika kao korisnik
public class UiTests : IClassFixture<UiTestServerFactory>, IAsyncLifetime
{
    private readonly UiTestServerFactory _factory;
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private IPage _page = null!;

    public UiTests(UiTestServerFactory factory)
    {
        _factory = factory;

        // Server se pokreće "lazy", pa ga ovdje pokrećemo da ServerAddress dobije vrijednost
        _ = _factory.Server;
    }

    // Prije SVAKOG testa: prazna baza, novi browser i otvorena stranica
    public async Task InitializeAsync()
    {
        _factory.ResetDatabase();

        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new() { Headless = true });
        _page = await _browser.NewPageAsync();
        await _page.GotoAsync(_factory.ServerAddress);
    }

    // Poslije svakog testa: zatvori browser
    public async Task DisposeAsync()
    {
        if (_browser != null) await _browser.CloseAsync();
        _playwright?.Dispose();
    }

    // Pomoćna metoda: upiše naziv i klikne "Dodaj"
    private async Task AddTaskAsync(string title)
    {
        await _page.FillAsync("#task-title", title);
        await _page.ClickAsync("#add-button");
    }

    [Fact]
    public async Task EmptyList_ShowsEmptyMessageAndZeroCounter()
    {
        await Expect(_page.GetByTestId("empty-message")).ToBeVisibleAsync();
        await Expect(_page.GetByTestId("task-count"))
            .ToHaveTextAsync("Ukupno: 0 | Završeno: 0");
    }

    [Fact]
    public async Task AddTask_ShowsTaskInListWithToDoStatus()
    {
        await AddTaskAsync("Napisati test plan");

        var item = _page.GetByTestId("task-item");
        await Expect(item).ToHaveCountAsync(1);
        await Expect(item.Locator(".title")).ToHaveTextAsync("Napisati test plan");
        await Expect(item.Locator(".badge")).ToHaveTextAsync("ToDo");
        await Expect(_page.GetByTestId("empty-message")).ToBeHiddenAsync();
    }

    [Fact]
    public async Task AddTask_EmptyTitle_ShowsErrorAndAddsNothing()
    {
        await _page.ClickAsync("#add-button");

        await Expect(_page.Locator("#error-message"))
            .ToHaveTextAsync("Naziv zadatka je obavezan.");
        await Expect(_page.GetByTestId("task-item")).ToHaveCountAsync(0);
    }

    [Fact]
    public async Task CompleteTask_MarksTaskAsDoneAndUpdatesCounter()
    {
        await AddTaskAsync("Završiti UI testove");

        var item = _page.GetByTestId("task-item");
        await item.Locator(".complete-button").ClickAsync();

        await Expect(item.Locator(".badge")).ToHaveTextAsync("Done");
        await Expect(item).ToHaveClassAsync(new Regex("done"));
        await Expect(item.Locator(".complete-button")).ToHaveCountAsync(0);
        await Expect(_page.GetByTestId("task-count"))
            .ToHaveTextAsync("Ukupno: 1 | Završeno: 1");
    }

    [Fact]
    public async Task DeleteTask_RemovesTaskAndShowsEmptyMessage()
    {
        await AddTaskAsync("Zadatak za brisanje");
        await Expect(_page.GetByTestId("task-item")).ToHaveCountAsync(1);

        await _page.ClickAsync(".delete-button");

        await Expect(_page.GetByTestId("task-item")).ToHaveCountAsync(0);
        await Expect(_page.GetByTestId("empty-message")).ToBeVisibleAsync();
    }
}