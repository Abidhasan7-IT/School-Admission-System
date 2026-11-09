using Microsoft.AspNetCore.Mvc;
using SchoolAdmission.Api.Contracts;
using SchoolAdmission.Api.Services;

namespace SchoolAdmission.Api.Controllers;

[ApiController]
[Route("classes")]
public class ClassesController(AdmissionService admissions) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateClassRequest request)
    {
        var created = await admissions.CreateClassAsync(request.Name ?? string.Empty, request.TotalSeats);
        return StatusCode(StatusCodes.Status201Created, created);
    }

    [HttpGet]
    public async Task<IActionResult> List() => Ok(await admissions.ListClassesAsync());

    [HttpGet("{classId}")]
    public async Task<IActionResult> Get(string classId) => Ok(await admissions.GetClassAsync(classId));

    [HttpPost("{classId}/holds")]
    public async Task<IActionResult> CreateHold(
        string classId,
        [FromHeader(Name = "X-Student-Id")] string? studentId)
    {
        var hold = await admissions.CreateHoldAsync(classId, StudentId.Require(studentId));
        return StatusCode(StatusCodes.Status201Created, hold);
    }
}
