using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace SchoolAdmission.Tests;

[Collection("api")]
public class HoldLifecycleTests(ApiFactory factory)
{
    [Fact]
    public async Task Apply_then_confirm_moves_seat_from_held_to_confirmed_and_is_idempotent()
    {
        var client = factory.CreateClient();
        var student = TestHttp.StudentId(101);
        var classId = await TestHttp.CreateClassAsync(client, "Lifecycle " + Guid.NewGuid(), totalSeats: 3);

        var created = await TestHttp.CreateHoldAsync(client, classId, student);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var hold = await created.Content.ReadFromJsonAsync<JsonElement>();
        var holdId = hold.GetProperty("holdId").GetString()!;
        Assert.Equal("HELD", hold.GetProperty("status").GetString());

        var confirm = await TestHttp.SendAsStudentAsync(client, HttpMethod.Post, $"/holds/{holdId}/confirm", student);
        Assert.Equal(HttpStatusCode.OK, confirm.StatusCode);

        // Confirming again must not count the seat twice.
        var confirmAgain = await TestHttp.SendAsStudentAsync(client, HttpMethod.Post, $"/holds/{holdId}/confirm", student);
        Assert.Equal(HttpStatusCode.OK, confirmAgain.StatusCode);

        var summary = await client.GetFromJsonAsync<JsonElement>($"/classes/{classId}");
        Assert.Equal(1, summary.GetProperty("confirmedSeats").GetInt32());
        Assert.Equal(0, summary.GetProperty("heldSeats").GetInt32());
        Assert.Equal(2, summary.GetProperty("availableSeats").GetInt32());
    }
}
