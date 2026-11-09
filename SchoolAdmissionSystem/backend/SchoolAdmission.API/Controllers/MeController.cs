using Microsoft.AspNetCore.Mvc;
using SchoolAdmission.Api.Services;

namespace SchoolAdmission.Api.Controllers;

[ApiController]
[Route("me")]
public class MeController(AdmissionService admissions) : ControllerBase
{
    [HttpGet("holds")]
    public async Task<IActionResult> MyHolds([FromHeader(Name = "X-Student-Id")] string? studentId) =>
        Ok(await admissions.GetMyHoldsAsync(StudentId.Require(studentId)));
}
