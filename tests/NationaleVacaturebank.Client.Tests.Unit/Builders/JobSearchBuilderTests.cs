using System.Net;
using System.Text;
using NationaleVacaturebank.Client.Client;
using NationaleVacaturebank.Client.Exceptions;
using NationaleVacaturebank.Client.Models;
using NationaleVacaturebank.Client.Models.Enums;
using NationaleVacaturebank.Client.Options;
using NationaleVacaturebank.Client.Tests.Unit.Helpers;

namespace NationaleVacaturebank.Client.Tests.Unit.Builders;

public class JobSearchBuilderTests
{
    private const string SingleJobPageJson = """
        {
          "page": 1, "limit": 10, "pages": 3, "total": 25,
          "_embedded": {
            "jobs": [
              {
                "id": "abc123", "title": "C# Developer",
                "categories": ["IT"], "industries": ["Technology"],
                "company": {"name": "Acme BV"}
              }
            ]
          },
          "_links": { "next": { "href": "https://api.nationalevacaturebank.nl/jobs?page=2" } }
        }
        """;

    private static VacaturebankClient CreateClient(string json,
        HttpStatusCode status = HttpStatusCode.OK)
    {
        var handler = new MockHttpMessageHandler(json, status);
        return new VacaturebankClient(new HttpClient(handler), new VacaturebankOptions());
    }

    [Fact]
    public async Task GetPageAsync_ReturnsJobsPage()
    {
        var client = CreateClient(SingleJobPageJson);

        var page = await client.Jobs().GetPageAsync(page: 1, limit: 10);

        Assert.Equal(1, page.Page);
        Assert.Equal(25, page.Total);
        Assert.Single(page.Jobs);
        Assert.Equal("abc123", page.Jobs[0].Id);
        Assert.Equal("C# Developer", page.Jobs[0].Title);
        Assert.Equal("Acme BV", page.Jobs[0].Company?.Name);
    }

    [Fact]
    public void HasNextPage_IsTrue_WhenNotOnLastPage()
    {
        var page = new JobsPage { Page = 1, Limit = 10, Pages = 3, Total = 25, Jobs = [] };
        Assert.True(page.HasNextPage);
    }

    [Fact]
    public void HasNextPage_IsFalse_OnLastPage()
    {
        var page = new JobsPage { Page = 3, Limit = 10, Pages = 3, Total = 25, Jobs = [] };
        Assert.False(page.HasNextPage);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetPageAsync_ThrowsValidation_WhenPageInvalid(int invalidPage)
    {
        var client = CreateClient(SingleJobPageJson);

        await Assert.ThrowsAsync<VacaturebankValidationException>(
            () => client.Jobs().GetPageAsync(page: invalidPage, limit: 10));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public async Task GetPageAsync_ThrowsValidation_WhenLimitOutOfRange(int invalidLimit)
    {
        var client = CreateClient(SingleJobPageJson);

        await Assert.ThrowsAsync<VacaturebankValidationException>(
            () => client.Jobs().GetPageAsync(page: 1, limit: invalidLimit));
    }

    [Fact]
    public async Task GetPageAsync_ThrowsValidation_WhenWithinKmSetWithoutCoordinates()
    {
        var client = CreateClient(SingleJobPageJson);

        await Assert.ThrowsAsync<VacaturebankValidationException>(
            () => client.Jobs().WithinKm(10).GetPageAsync());
    }

    [Fact]
    public async Task GetPageAsync_DoesNotThrow_WhenWithinKmAndCoordinatesBothSet()
    {
        var client = CreateClient(SingleJobPageJson);

        var page = await client.Jobs()
            .AtCoordinates(52.37, 4.89)
            .WithinKm(10)
            .GetPageAsync();

        Assert.NotNull(page);
    }

    [Fact]
    public async Task GetPageAsync_SendsCorrectQueryParameters()
    {
        HttpRequestMessage? captured = null;
        var handler = new MockHttpMessageHandler(req =>
        {
            captured = req;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(SingleJobPageJson, Encoding.UTF8, "application/json")
            };
        });
        var client = new VacaturebankClient(new HttpClient(handler), new VacaturebankOptions());

        await client.Jobs()
            .WithTitle("Developer")
            .InCity("Amsterdam")
            .SortBy(JobSort.Date)
            .GetPageAsync(page: 1, limit: 5);

        Assert.NotNull(captured);
        var url = Uri.UnescapeDataString(captured!.RequestUri!.ToString());
        Assert.Contains("sort=date", url);
        Assert.Contains("limit=5", url);
        Assert.Contains("dcoTitle:Developer", url);
        Assert.Contains("city:Amsterdam", url);
    }

    [Fact]
    public async Task GetAllAsync_YieldsAllJobsAcrossPages()
    {
        var page1 = """
            {
              "page": 1, "limit": 1, "pages": 2, "total": 2,
              "_embedded": { "jobs": [{"id": "job1", "title": "Job 1", "categories": [], "industries": []}] },
              "_links": { "next": { "href": "..." } }
            }
            """;
        var page2 = """
            {
              "page": 2, "limit": 1, "pages": 2, "total": 2,
              "_embedded": { "jobs": [{"id": "job2", "title": "Job 2", "categories": [], "industries": []}] },
              "_links": {}
            }
            """;
        var callCount = 0;
        var handler = new MockHttpMessageHandler(_ =>
        {
            var json = ++callCount == 1 ? page1 : page2;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
        });
        var client = new VacaturebankClient(new HttpClient(handler), new VacaturebankOptions());

        var jobs = new List<Job>();
        await foreach (var job in client.Jobs().GetAllAsync(limit: 1))
            jobs.Add(job);

        Assert.Equal(2, jobs.Count);
        Assert.Equal("job1", jobs[0].Id);
        Assert.Equal("job2", jobs[1].Id);
    }
}
