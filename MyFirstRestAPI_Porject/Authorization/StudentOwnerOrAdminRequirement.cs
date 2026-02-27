using Microsoft.AspNetCore.Authorization;

namespace StudentApi.Authorization;

public sealed class StudentOwnerOrAdminRequirement : IAuthorizationRequirement { }
