using System.Net.Http.Json;
using System.Text.Json;

namespace SchoolAdmission.Tests;

internal static class TestHttp
{
    /// <summary>Builds a valid 17-digit student ID for test index n.</summary>
    public static string StudentId(int n) => "2015269123456" + n.ToString("D4");

    public static async Task<string> CreateClassAsync(HttpClient client, string name, int totalSeats)
    {
        var response = await client.PostAsJsonAsync("/classes", new { name, totalSeats });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("id").GetString()!;
    }

    public static Task<HttpResponseMessage> CreateHoldAsync(HttpClient client, string classId, string studentId)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"/classes/{classId}/holds");
        request.Headers.Add("X-Student-Id", studentId);
        return client.SendAsync(request);
    }

    public static Task<HttpResponseMessage> SendAsStudentAsync(HttpClient client, HttpMethod method, string path, string studentId)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.Add("X-Student-Id", studentId);
        return client.SendAsync(request);
    }
}
