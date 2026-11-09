using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace SchoolAdmission.Tests;

[Collection("api")]
public class ApplicationRulesTests(ApiFactory factory)
{
    [Fact]
    public async Task Student_cannot_hold_two_seats_in_one_class_and_cannot_touch_others_holds()
    {
        var client = factory.CreateClient();
        var owner = TestHttp.StudentId(201);
        var other = TestHttp.StudentId(202);
        var classId = await TestHttp.CreateClassAsync(client, "Rules " + Guid.NewGuid(), totalSeats: 10);

        var first = await TestHttp.CreateHoldAsync(client, classId, owner);
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        var holdId = (await first.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("holdId").GetString()!;

        // Same student, same class, second application is refused.
        var duplicate = await TestHttp.CreateHoldAsync(client, classId, owner);
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);

        // Another student cannot confirm this hold.
        var stolen = await TestHttp.SendAsStudentAsync(client, HttpMethod.Post, $"/holds/{holdId}/confirm", other);
        Assert.Equal(HttpStatusCode.Forbidden, stolen.StatusCode);

        // Invalid student ID is a 4xx, never a 500.
        var invalid = await TestHttp.SendAsStudentAsync(client, HttpMethod.Get, "/me/holds", "12345");
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
    }

    [Fact]
    public async Task Cancelling_a_held_application_gives_the_seat_back()
    {
        var client = factory.CreateClient();
        var student = TestHttp.StudentId(301);
        var classId = await TestHttp.CreateClassAsync(client, "Cancel " + Guid.NewGuid(), totalSeats: 1);

        var created = await TestHttp.CreateHoldAsync(client, classId, student);
        var holdId = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("holdId").GetString()!;

        var full = await TestHttp.CreateHoldAsync(client, classId, TestHttp.StudentId(302));
        Assert.Equal(HttpStatusCode.Conflict, full.StatusCode);

        var cancel = await TestHttp.SendAsStudentAsync(client, HttpMethod.Delete, $"/holds/{holdId}", student);
        Assert.Equal(HttpStatusCode.NoContent, cancel.StatusCode);

        var retry = await TestHttp.CreateHoldAsync(client, classId, TestHttp.StudentId(302));
        Assert.Equal(HttpStatusCode.Created, retry.StatusCode);
    }
}
