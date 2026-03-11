using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentApi.DataSimulation;
using StudentApi.Model;

namespace StudentApi.Controllers;

[Authorize]
[ApiController]
[Route("api/Students")]
public class StudentsController(Logger<StudentsController> logger) : ControllerBase
{
	private const int MinimumAcceptedAge = 7;
	private const int MinimumAcceptedGrade = 0;
	private const int MaximumAcceptedGrade = 100;

	[Authorize(Roles = "Admin")]
	[HttpGet("All", Name = "GetAllStudents")]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	[ProducesResponseType(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType(StatusCodes.Status403Forbidden)]
	public ActionResult<IEnumerable<Student>> GetAllStudents()
	{
		if (StudentDataSimulation.StudentsList.Count is 0)
		{
			return NotFound("No Students Found!");
		}
		return Ok(StudentDataSimulation.StudentsList);
	}


	[AllowAnonymous]
	[HttpGet("Passed", Name = "GetPassedStudents")]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public ActionResult<IEnumerable<Student>> GetPassedStudents()
	{
		List<Student> passedStudents = StudentDataSimulation.StudentsList
			.Where(student => student.Grade >= 50)
			.ToList();

		if (passedStudents.Count is 0)
		{
			return NotFound("No Students Passed");
		}

		return Ok(passedStudents);
	}


	[AllowAnonymous]
	[HttpGet("AverageGrade", Name = "GetAverageGrade")]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public ActionResult<double> GetAverageGrade()
	{
		if (StudentDataSimulation.StudentsList.Count is 0)
		{
			return NotFound("No students found.");
		}

		double averageGrade = StudentDataSimulation.StudentsList
			.Average(student => student.Grade);

		return Ok(averageGrade);
	}


	[Authorize]
	[HttpGet("{id:int}", Name = "GetStudentById")]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	[ProducesResponseType(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType(StatusCodes.Status403Forbidden)]
	public async Task<ActionResult<Student>> GetStudentById(int id,
		[FromServices] IAuthorizationService authorizationService)
	{
		var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
		
		if (id < 1)
		{
			logger.LogWarning("Retrieved student info failed (invalid id). Method={MethodName}, TargetId={TargetId}, IP={IP}",
				nameof(GetStudentById),
				id,
				ip);
			
			return BadRequest($"Not accepted ID {id}");
		}

		Student? student = StudentDataSimulation.StudentsList.FirstOrDefault(s => s.Id == id);
		if (student is null)
		{
			logger.LogWarning("Retrieved student info failed (target not found). Method={MethodName}, TargetId={TargetId}, IP={IP}",
				nameof(GetStudentById),
				id,
				ip);
			
			return NotFound($"Student with ID {id} not found.");
		}

		AuthorizationResult authResult = await authorizationService.AuthorizeAsync(
			user: User,
			resource: id,
			policyName: "StudentOwnerOrAdmin");

		return authResult.Succeeded
			? Ok(student)
			: Forbid();
	}


	[Authorize(Roles = "Admin")]
	[HttpPost(Name = "AddStudent")]
	[ProducesResponseType(StatusCodes.Status201Created)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	[ProducesResponseType(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType(StatusCodes.Status403Forbidden)]
	public ActionResult<Student> AddStudent(Student newStudent)
	{
		var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
		var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown";
		
		if (newStudent is { Name: null or "" } || newStudent.Age < MinimumAcceptedAge ||
			newStudent.Grade < MinimumAcceptedGrade || newStudent.Grade > MaximumAcceptedGrade)
		{
			logger.LogWarning(
				"Admin action blocked (invalid student info). AdminId={AdminId}, Action=AddStudent, TargetEmail={TargetMaskedEmail}, IP={IP}",
				adminId,
				AuthController.MaskEmail(newStudent.Email),
				ip);
			
			return BadRequest("Invalid student data.");
		}

		const int minimumUserId = 1;

		logger.LogWarning(
			"Admin action started. AdminId={AdminId}, Action=AddStudent, TargetEmail={TargetMaskedEmail}, IP={IP}",
			adminId,
			AuthController.MaskEmail(newStudent.Email),
			ip);
		
		newStudent.Id = StudentDataSimulation.StudentsList.Count > 0
			? StudentDataSimulation.StudentsList.Max(s => s.Id) + 1
			: minimumUserId;

		StudentDataSimulation.StudentsList.Add(newStudent);

		logger.LogWarning(
			"Admin action succeeded. AdminId={AdminId}, Action=AddStudent, TargetEmail={TargetMaskedEmail}, IP={IP}",
			adminId,
			AuthController.MaskEmail(newStudent.Email),
			ip);
		
		//we don't return Ok here,we return createdAtRoute: this will be status code 201 created.
		return CreatedAtRoute("GetStudentById", new { id = newStudent.Id }, newStudent);
	}


	[Authorize(Roles = "Admin")]
	[HttpPut("{id:int}", Name = "UpdateStudent")]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	[ProducesResponseType(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType(StatusCodes.Status403Forbidden)]
	public ActionResult<Student> UpdateStudent(int id, Student updatedStudent)
	{
		var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
		var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown";
		
		if (id < 1 || updatedStudent is { Name: null or "" } ||
			updatedStudent.Age < MinimumAcceptedAge ||
			updatedStudent.Grade < MinimumAcceptedGrade || updatedStudent.Grade > MaximumAcceptedGrade)
		{
			logger.LogWarning(
				"Admin action blocked (invalid student info). AdminId={AdminId}, Action=UpdateStudent, TargetId={TargetId}, IP={IP}",
				adminId,
				id,
				ip);
			
			return BadRequest("Invalid student data.");
		}

		Student? student = StudentDataSimulation.StudentsList.FirstOrDefault(s => s.Id == id);
		if (student is null)
		{
			logger.LogWarning(
				"Admin action blocked (target not found). AdminId={AdminId}, Action=UpdateStudent, TargetId={TargetId}, IP={IP}",
				adminId,
				id,
				ip);
			
			return NotFound($"Student with ID {id} not found.");
		}

		logger.LogWarning(
				"Admin action started. AdminId={AdminId}, Action=UpdateStudent, TargetId={TargetId}, IP={IP}",
				adminId,
				id,
				ip);
		
		student.Name = updatedStudent.Name;
		student.Age = updatedStudent.Age;
		student.Grade = updatedStudent.Grade;

		logger.LogWarning(
				"Admin action succeeded. AdminId={AdminId}, Action=UpdateStudent, TargetId={TargetId}, IP={IP}",
				adminId,
				id,
				ip);
		
		return Ok(student);
	}


	[Authorize(Roles = "Admin")]
	[HttpDelete("{id:int}", Name = "DeleteStudent")]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	[ProducesResponseType(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType(StatusCodes.Status403Forbidden)]
	public ActionResult DeleteStudent(int id)
	{
		var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
		var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown";
		
		if (id < 1)
		{
			logger.LogWarning(
				"Admin action blocked (invalid id). AdminId={AdminId}, Action=DeleteStudent, TargetId={TargetId}, IP={IP}",
				adminId,
				id,
				ip);
			
			return BadRequest($"Not accepted ID {id}");
		}

		Student? student = StudentDataSimulation.StudentsList.FirstOrDefault(s => s.Id == id);
		if (student is null)
		{
			logger.LogWarning(
				"Admin action failed (target not found). AdminId={AdminId}, Action=DeleteStudent, TargetId={TargetId}, IP={IP}",
				adminId,
				id,
				ip);
			
			return NotFound($"Student with ID {id} not found.");
		}

		logger.LogWarning(
				"Admin action started. AdminId={AdminId}, Action=DeleteStudent, TargetId={TargetId}, IP={IP}",
				adminId,
				id,
				ip);
		
		StudentDataSimulation.StudentsList.Remove(student);
		
		logger.LogWarning(
				"Admin action succeeded. AdminId={AdminId}, Action=DeleteStudent, TargetId={TargetId}, IP={IP}",
				adminId,
				id,
				ip);
		
		return Ok($"Student with ID {id} has been deleted.");
	}
}
