using System.Text.RegularExpressions;
using SchoolAdmission.Api.Infrastructure;

namespace SchoolAdmission.Api.Services;

public static partial class StudentId
{
    // [0-9] instead of \d: \d would also accept Bengali digits (০-৯).
    [GeneratedRegex("^[0-9]{17}$")]
    private static partial Regex Pattern();

    public static string Require(string? value)
    {
        if (value is null || !Pattern().IsMatch(value))
        {
            throw new ApiException(
                StatusCodes.Status400BadRequest,
                "INVALID_STUDENT_ID",
                "X-Student-Id must be the student's 17-digit birth registration number.");
        }

        return value;
    }
}
