using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace SchoolAdmission.Tests;

[Collection("api")]
public class ConcurrencyTests(ApiFactory factory)
{
    [Fact]
    public async Task Parallel_applications_never_hold_more_seats_than_exist()
    {
        var client = factory.CreateClient();
        var classId = await TestHttp.CreateClassAsync(client, "Concurrency " + Guid.NewGuid(), totalSeats: 5);

        // 30 students apply at the same moment for 5 seats.
        var attempts = Enumerable.Range(0, 30)
            .Select(i => TestHttp.CreateHoldAsync(client, classId, TestHttp.StudentId(i)));
        var responses = await Task.WhenAll(attempts);

        Assert.Equal(5, responses.Count(r => r.StatusCode == HttpStatusCode.Created));
        Assert.Equal(25, responses.Count(r => r.StatusCode == HttpStatusCode.Conflict));

        var summary = await client.GetFromJsonAsync<JsonElement>($"/classes/{classId}");
        Assert.Equal(5, summary.GetProperty("heldSeats").GetInt32());
        Assert.Equal(0, summary.GetProperty("availableSeats").GetInt32());
    }
}
