using Application.DTOs.Accounting;
using Application.Services.Accounting;
using Domain.Enums;
using Domain.Models.Accounting;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Tests.Accounting;

public class ExpenseServiceTests : IDisposable
{
    private readonly ApplicationDbContext _ctx;
    private readonly ExpenseService _svc;

    public ExpenseServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _ctx = new ApplicationDbContext(options);
        _svc = new ExpenseService(_ctx);
    }

    public void Dispose() => _ctx.Dispose();

    [Fact]
    public async Task CreateAsync_PersistsExpense()
    {
        var result = await _svc.CreateAsync(new CreateExpenseDto
        {
            Title = "إيجار يناير",
            Category = ExpenseCategory.Rent,
            Amount = 5000m,
            PaymentMethod = PaymentMethod.BankTransfer,
        }, userId: null);

        result.Id.Should().NotBe(Guid.Empty);
        result.Title.Should().Be("إيجار يناير");
        (await _ctx.Expenses.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task GetAllAsync_FiltersByDateRange()
    {
        var now = DateTime.UtcNow;
        _ctx.Expenses.AddRange(
            new Expense { Title = "Old", Amount = 100, ExpenseDate = now.AddDays(-40) },
            new Expense { Title = "Recent", Amount = 200, ExpenseDate = now.AddDays(-5) });
        await _ctx.SaveChangesAsync();

        var list = await _svc.GetAllAsync(new ExpenseFilterDto
        {
            From = now.AddDays(-10),
            To = now,
        });
        list.Should().HaveCount(1);
        list[0].Title.Should().Be("Recent");
    }

    [Fact]
    public async Task GetAllAsync_FiltersByCategory()
    {
        _ctx.Expenses.AddRange(
            new Expense { Title = "Rent", Amount = 1, Category = ExpenseCategory.Rent },
            new Expense { Title = "Tax", Amount = 2, Category = ExpenseCategory.Tax });
        await _ctx.SaveChangesAsync();

        var list = await _svc.GetAllAsync(new ExpenseFilterDto
        {
            Category = ExpenseCategory.Tax,
        });
        list.Should().ContainSingle(e => e.Title == "Tax");
    }

    [Fact]
    public async Task GetSummaryAsync_AggregatesByCategory()
    {
        var now = DateTime.UtcNow;
        _ctx.Expenses.AddRange(
            new Expense { Title = "Rent1", Amount = 1000, Category = ExpenseCategory.Rent, ExpenseDate = now },
            new Expense { Title = "Rent2", Amount = 500, Category = ExpenseCategory.Rent, ExpenseDate = now },
            new Expense { Title = "Tax",   Amount = 200, Category = ExpenseCategory.Tax,  ExpenseDate = now });
        await _ctx.SaveChangesAsync();

        var summary = await _svc.GetSummaryAsync(now.AddDays(-1), now.AddDays(1));
        summary.Total.Should().Be(1700);
        summary.Count.Should().Be(3);
        summary.ByCategory.Should().HaveCount(2);
        summary.ByCategory[0].Category.Should().Be(ExpenseCategory.Rent);
        summary.ByCategory[0].Total.Should().Be(1500); // sorted DESC by total
    }

    [Fact]
    public async Task DeleteAsync_RemovesExpense()
    {
        var created = await _svc.CreateAsync(new CreateExpenseDto
        {
            Title = "X", Amount = 1, Category = ExpenseCategory.Other,
        }, userId: null);

        var deleted = await _svc.DeleteAsync(created.Id);
        deleted.Should().BeTrue();
        (await _ctx.Expenses.CountAsync()).Should().Be(0);
    }
}
