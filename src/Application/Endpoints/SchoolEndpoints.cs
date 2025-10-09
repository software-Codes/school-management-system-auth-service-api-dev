using AuthService.Abstractions.Common;
using AuthService.Application.Contracts.School;
using AuthService.Infrastructure.Auth;
using AuthService.Infrastructure.Persistence.EfCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;


namespace AuthService.Application.Endpoints;


public static class SchoolEndpoints
{
    public static IEndpointRouteBuilder MapSchoolEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/schools")
            .WithTags("Schools")
            .WithOpenApi();

        // Only SystemAdmin can register schools
        group.MapPost("/", RegisterSchool)
            .WithName("RegisterSchool")
            .RequireAuthorization(AuthorizationPolicies.SystemAdminOnly)
            .Produces<SchoolResponse>(201)
            .Produces<ProblemDetails>(400)
            .Produces<ProblemDetails>(409);

        // List all schools (authenticated users)
        group.MapGet("/", GetSchools)
            .WithName("GetSchools")
            .RequireAuthorization(AuthorizationPolicies.RequireAuthentication)
            .Produces<SchoolListResponse>(200);

        // Get single school by ID or slug
        group.MapGet("/{idOrSlug}", GetSchool)
            .WithName("GetSchool")
            .RequireAuthorization(AuthorizationPolicies.RequireAuthentication)
            .Produces<SchoolResponse>(200)
            .Produces<ProblemDetails>(404);

        group.MapPut("/{idOrSlug}", UpdateSchool)
           .WithName("UpdateSchool")
           .RequireAuthorization(AuthorizationPolicies.SystemAdminOnly)
           .Produces<SchoolResponse>(200)
           .Produces<ProblemDetails>(400)
           .Produces<ProblemDetails>(404)
           .Produces<ProblemDetails>(409);

        group.MapPatch("/{idOrSlug}", PatchSchool)
           .WithName("PatchSchool")
           .RequireAuthorization(AuthorizationPolicies.SystemAdminOnly)
           .Produces<SchoolResponse>(200)
           .Produces<ProblemDetails>(400)
           .Produces<ProblemDetails>(404)
           .Produces<ProblemDetails>(409);

        group.MapDelete("/{idOrSlug}", DeleteSchool)
           .WithName("DeleteSchool")
           .RequireAuthorization(AuthorizationPolicies.SystemAdminOnly)
           .Produces(204)
           .Produces<ProblemDetails>(404);

        return endpoints;

    }

    private static async Task<IResult> RegisterSchool(
        [FromBody] RegisterSchoolRequest request,
        [FromServices] IdentityDbContext context,
        [FromServices] IDateTimeProvider dateTimeProvider,
        CancellationToken cancellationToken

    )
    {
        // Validate slug uniqueness
        var slugExists = await context.Schools
            .AnyAsync(s => s.Slug == request.Slug.ToLowerInvariant(), cancellationToken);

        if (slugExists)
        {
            return Results.Problem(
                detail: $"A school with slug '{request.Slug}' already exists",
                statusCode: 409,
                title: "Duplicate School");
        }
        // Validate EMIS code uniqueness (if provided)
        if (!string.IsNullOrWhiteSpace(request.EmisCode))
        {
            var emisExists = await context.Schools
                .AnyAsync(s => s.EmisCode == request.EmisCode, cancellationToken);

            if (emisExists)
            {
                return Results.Problem(
                    detail: $"A school with EMIS code '{request.EmisCode}' already exists",
                    statusCode: 409,
                    title: "Duplicate EMIS Code");
            }
        }

        // Create school
        var school = Domain.Entities.School.Create(
            slug: request.Slug,
            officialName: request.OfficialName,
            emisCode: request.EmisCode,
            location: request.Location,
            utcNow: dateTimeProvider.UtcNow,
            email: request.Email,
            phone: request.Phone,
            address: request.Address
        );

        await context.Schools.AddAsync(school, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        var response = MapToResponse(school);

        return Results.Created($"/api/schools/{school.Slug}", response);


    }

    private static async Task<IResult> GetSchools(
        [FromServices] IdentityDbContext context,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null,
        CancellationToken cancellationToken = default)
    {
        //validate page and pageSize

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = context.Schools.AsQueryable();

        // Filter by status if provided
        if (!string.IsNullOrWhiteSpace(status) &&
            Enum.TryParse<Domain.Enums.SchoolStatus>(status, true, out var schoolStatus))
        {
            query = query.Where(s => s.Status == schoolStatus);
        }
        var totalCount = await query.CountAsync(cancellationToken);
        var schools = await query
            .OrderBy(s => s.OfficialName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => MapToResponse(s))
            .ToListAsync(cancellationToken);

        var response = new SchoolListResponse
        {
            Schools = schools,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };

        return Results.Ok(response);


    }
    // GetSchool (lines 149-179)
    private static async Task<IResult> GetSchool(
        string idOrSlug,
        [FromServices] IdentityDbContext context,
        CancellationToken cancellationToken)
    {
        var school = await FindSchoolAsync(idOrSlug, context, cancellationToken);

        if (school == null)
        {
            return Results.Problem(
                detail: $"School with identifier '{idOrSlug}' not found",
                statusCode: 404,
                title: "School Not Found");
        }

        var response = MapToResponse(school);
        return Results.Ok(response);
    }

    private static async Task<IResult> UpdateSchool(
    string idOrSlug,
    [FromBody] UpdateSchoolRequest request,
    [FromServices] IdentityDbContext context,
    [FromServices] IDateTimeProvider dateTimeProvider,
    CancellationToken cancellationToken)
    {
        var school = await FindSchoolAsync(idOrSlug, context, cancellationToken);

        if (school == null)
        {
            return Results.Problem(
                detail: $"School with identifier '{idOrSlug}' not found",
                statusCode: 404,
                title: "School Not Found");
        }

        // Validate EMIS code uniqueness if it's being changed
        if (!string.IsNullOrWhiteSpace(request.EmisCode) &&
            request.EmisCode != school.EmisCode)
        {
            var emisExists = await context.Schools
                .AnyAsync(s => s.EmisCode == request.EmisCode && s.Id != school.Id,
                         cancellationToken);

            if (emisExists)
            {
                return Results.Problem(
                    detail: $"A school with EMIS code '{request.EmisCode}' already exists",
                    statusCode: 409,
                    title: "Duplicate EMIS Code");
            }
        }
        try
        {
            school.UpdateDetails(
                officialName: request.OfficialName,
                location: request.Location,
                emisCode: request.EmisCode,
                email: request.Email,
                phone: request.Phone,
                address: request.Address,
                utcNow: dateTimeProvider.UtcNow
            );

            await context.SaveChangesAsync(cancellationToken);

            var response = MapToResponse(school);
            return Results.Ok(response);
        }
        catch (ArgumentException ex)
        {
            return Results.Problem(
                detail: ex.Message,
                statusCode: 400,
                title: "Invalid Input");
        }

    }

    private static async Task<IResult> PatchSchool(
        string idOrSlug,
        [FromBody] PatchSchoolRequest request,
        [FromServices] IdentityDbContext context,
        [FromServices] IDateTimeProvider dateTimeProvider,
        CancellationToken cancellationToken)
    {
        var school = await FindSchoolAsync(idOrSlug, context, cancellationToken);

        if (school == null)
        {
            return Results.Problem(
                detail: $"School with identifier '{idOrSlug}' not found",
                statusCode: 404,
                title: "School Not Found");
        }

        // Validate EMIS code uniqueness if it's being updated
        if (!string.IsNullOrWhiteSpace(request.EmisCode) &&
            request.EmisCode != school.EmisCode)
        {
            var emisExists = await context.Schools
                .AnyAsync(s => s.EmisCode == request.EmisCode && s.Id != school.Id,
                         cancellationToken);

            if (emisExists)
            {
                return Results.Problem(
                    detail: $"A school with EMIS code '{request.EmisCode}' already exists",
                    statusCode: 409,
                    title: "Duplicate EMIS Code");
            }
        }

        try
        {
            var hasChanges = false;

            // Update individual fields only if provided
            if (!string.IsNullOrWhiteSpace(request.OfficialName))
            {
                school.UpdateDetails(
                    officialName: request.OfficialName,
                    location: school.Location,
                    emisCode: school.EmisCode,
                    email: school.Email,
                    phone: school.Phone,
                    address: school.Address,
                    utcNow: dateTimeProvider.UtcNow
                );
                hasChanges = true;
            }

            if (!string.IsNullOrWhiteSpace(request.Location))
            {
                school.UpdateDetails(
                    officialName: school.OfficialName,
                    location: request.Location,
                    emisCode: school.EmisCode,
                    email: school.Email,
                    phone: school.Phone,
                    address: school.Address,
                    utcNow: dateTimeProvider.UtcNow
                );
                hasChanges = true;
            }

            if (request.EmisCode != null)
            {
                school.UpdateDetails(
                    officialName: school.OfficialName,
                    location: school.Location,
                    emisCode: request.EmisCode,
                    email: school.Email,
                    phone: school.Phone,
                    address: school.Address,
                    utcNow: dateTimeProvider.UtcNow
                );
                hasChanges = true;
            }

            if (request.Email != null || request.Phone != null)
            {
                school.UpdateContactInfo(
                    email: request.Email ?? school.Email,
                    phone: request.Phone ?? school.Phone,
                    utcNow: dateTimeProvider.UtcNow
                );
                hasChanges = true;
            }

            if (request.Address != null)
            {
                school.UpdateDetails(
                    officialName: school.OfficialName,
                    location: school.Location,
                    emisCode: school.EmisCode,
                    email: school.Email,
                    phone: school.Phone,
                    address: request.Address,
                    utcNow: dateTimeProvider.UtcNow
                );
                hasChanges = true;
            }

            // Update status if provided
            if (!string.IsNullOrWhiteSpace(request.Status))
            {
                if (Enum.TryParse<Domain.Enums.SchoolStatus>(request.Status, true, out var status))
                {
                    switch (status)
                    {
                        case Domain.Enums.SchoolStatus.Active:
                            school.Activate(dateTimeProvider.UtcNow);
                            break;
                        case Domain.Enums.SchoolStatus.Suspended:
                            school.Suspend(dateTimeProvider.UtcNow);
                            break;
                        case Domain.Enums.SchoolStatus.Closed:
                            school.Close(dateTimeProvider.UtcNow);
                            break;
                    }
                    hasChanges = true;
                }
                else
                {
                    return Results.Problem(
                        detail: $"Invalid status value. Must be one of: Active, Suspended, Closed",
                        statusCode: 400,
                        title: "Invalid Status");
                }
            }

            if (hasChanges)
            {
                await context.SaveChangesAsync(cancellationToken);
            }

            var response = MapToResponse(school);
            return Results.Ok(response);
        }
        catch (ArgumentException ex)
        {
            return Results.Problem(
                detail: ex.Message,
                statusCode: 400,
                title: "Invalid Input");
        }
    }

    private static async Task<IResult> DeleteSchool(
        string idOrSlug,
        [FromServices] IdentityDbContext context,
        CancellationToken cancellationToken)
    {
        var school = await FindSchoolAsync(idOrSlug, context, cancellationToken);

        if (school == null)
        {
            return Results.Problem(
                detail: $"School with identifier '{idOrSlug}' not found",
                statusCode: 404,
                title: "School Not Found");
        }

        context.Schools.Remove(school);
        await context.SaveChangesAsync(cancellationToken);

        return Results.NoContent();
    }




    // Helper method to prevent repetition

    private static async Task<Domain.Entities.School?> FindSchoolAsync(
    string idOrSlug,
    IdentityDbContext context,
    CancellationToken cancellationToken)
    {
        // Try to parse as GUID first
        if (Guid.TryParse(idOrSlug, out var schoolId))
        {
            var school = await context.Schools.FindAsync(new object[] { schoolId }, cancellationToken);
            if (school != null) return school;
        }

        // Otherwise search by slug
        return await context.Schools
            .FirstOrDefaultAsync(s => s.Slug == idOrSlug.ToLowerInvariant(), cancellationToken);
    }
    private static SchoolResponse MapToResponse(Domain.Entities.School school)
    {
        return new SchoolResponse
        {
            Id = school.Id,
            Slug = school.Slug,
            OfficialName = school.OfficialName,
            EmisCode = school.EmisCode ?? string.Empty,
            Location = school.Location,
            Email = school.Email,
            Phone = school.Phone,
            Address = school.Address,
            Status = school.Status.ToString(),
            CreatedAt = school.CreatedAtUtc
        };
    }
}