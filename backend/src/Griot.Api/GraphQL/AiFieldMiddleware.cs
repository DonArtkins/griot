using Griot.Api.Auth;
using HotChocolate.Resolvers;

namespace Griot.Api.GraphQL;

/// <summary>Every GraphQL root field is default-deny for AI, including newly added fields.</summary>
public sealed class AiFieldMiddleware(FieldDelegate next)
{
    public async Task InvokeAsync(IMiddlewareContext context)
    {
        var user = context.Service<IHttpContextAccessor>().HttpContext?.User;
        if (user is not null && AiAccess.IsAi(user))
        {
            var field = context.Selection.Field.Name;
            var isRoot = context.ObjectType == context.Schema.QueryType
                || context.ObjectType == context.Schema.MutationType;
            var scope = context.ObjectType == context.Schema.MutationType
                ? field switch
                {
                    "createTask" => ServiceTokenHandler.ScopeCreateTask,
                    "addComment" => ServiceTokenHandler.ScopeAddComment,
                    _ => null
                }
                : field switch
                {
                    "workspace" or "projects" or "board" or "tasks" or "task"
                        or "comments" or "activityFeed" or "dashboardSummary"
                        => ServiceTokenHandler.ScopeReadWorkspace,
                    _ => null
                };
            // UserType is also returned through nested assignee/author fields.
            if ((isRoot && !AiAccess.AllowsScope(user, scope)) || field == "twoFactorMethod")
                throw new GraphQLException(ErrorBuilder.New()
                    .SetMessage("Operation is outside the AI delegation.")
                    .SetCode("FORBIDDEN").Build());
        }
        await next(context);
    }
}
