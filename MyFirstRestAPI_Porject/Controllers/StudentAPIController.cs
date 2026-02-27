using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentApi.DataSimulation;
using StudentApi.Model;

namespace StudentApi.Controllers;

[Authorize]
[ApiController]
[Route("api/Students")]
public class StudentsController : ControllerBase
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
		if (id < 1)
		{
			return BadRequest($"Not accepted ID {id}");
		}

		Student? student = StudentDataSimulation.StudentsList.FirstOrDefault(s => s.Id == id);
		if (student is null)
		{
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
		if (newStudent is { Name: null or "" } || newStudent.Age < MinimumAcceptedAge ||
			newStudent.Grade < MinimumAcceptedGrade || newStudent.Grade > MaximumAcceptedGrade)
		{
			return BadRequest("Invalid student data.");
		}

		const int minimumUserId = 1;

		newStudent.Id = StudentDataSimulation.StudentsList.Count > 0
			? StudentDataSimulation.StudentsList.Max(s => s.Id) + 1
			: minimumUserId;

		StudentDataSimulation.StudentsList.Add(newStudent);

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
		if (id < 1 || updatedStudent is { Name: null or "" } ||
			updatedStudent.Age < MinimumAcceptedAge ||
			updatedStudent.Grade < MinimumAcceptedGrade || updatedStudent.Grade > MaximumAcceptedGrade)
		{
			return BadRequest("Invalid student data.");
		}

		Student? student = StudentDataSimulation.StudentsList.FirstOrDefault(s => s.Id == id);
		if (student is null)
		{
			return NotFound($"Student with ID {id} not found.");
		}

		student.Name = updatedStudent.Name;
		student.Age = updatedStudent.Age;
		student.Grade = updatedStudent.Grade;

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
		if (id < 1)
		{
			return BadRequest($"Not accepted ID {id}");
		}

		Student? student = StudentDataSimulation.StudentsList.FirstOrDefault(s => s.Id == id);
		if (student is null)
		{
			return NotFound($"Student with ID {id} not found.");
		}

		StudentDataSimulation.StudentsList.Remove(student);
		return NoContent();
	}
}
