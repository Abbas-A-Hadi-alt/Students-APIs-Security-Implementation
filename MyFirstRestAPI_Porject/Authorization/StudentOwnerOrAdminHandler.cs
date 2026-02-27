using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace StudentApi.Authorization;

public sealed class StudentOwnerOrAdminHandler :
	AuthorizationHandler<StudentOwnerOrAdminRequirement, int>
{
	protected override Task HandleRequirementAsync(
		AuthorizationHandlerContext context,
		StudentOwnerOrAdminRequirement requirement,
		int studentId)
	{
		// Admin Override
		if (context.User.IsInRole("Admin"))
		{
			context.Succeed(requirement);
			return Task.CompletedTask;
		}

		// Ownership Check
		string? userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);

		if (int.TryParse(userId, out int authorizedStudentId) && authorizedStudentId == studentId)
		{
			context.Succeed(requirement);
		}

		return Task.CompletedTask;
	}
}
