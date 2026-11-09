using Microsoft.AspNetCore.Mvc;
using SchoolAdmission.Api.Services;

namespace SchoolAdmission.Api.Controllers;

[ApiController]
[Route("holds")]
public class HoldsController(AdmissionService admissions) : ControllerBase
{
    [HttpGet("{holdId}")]
    public async Task<IActionResult> Get(string holdId) => Ok(await admissions.GetHoldAsync(holdId));

    [HttpPost("{holdId}/confirm")]
    public async Task<IActionResult> Confirm(
        string holdId,
        [FromHeader(Name = "X-Student-Id")] string? studentId) =>
        Ok(await admissions.ConfirmHoldAsync(holdId, StudentId.Require(studentId)));

    [HttpDelete("{holdId}")]
    public async Task<IActionResult> Cancel(
        string holdId,
        [FromHeader(Name = "X-Student-Id")] string? studentId)
    {
        await admissions.CancelHoldAsync(holdId, StudentId.Require(studentId));
        return NoContent();
    }
}
