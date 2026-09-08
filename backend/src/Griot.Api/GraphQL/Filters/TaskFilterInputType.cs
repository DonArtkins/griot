using Griot.Api.GraphQL.Types;
using Griot.Domain.Enums;
using HotChocolate.Data.Filters;

namespace Griot.Api.GraphQL.Filters;

public class TaskFilterInputType : FilterInputType<TaskItemType>
{
    protected override void Configure(IFilterInputTypeDescriptor<TaskItemType> descriptor)
    {
        descriptor.BindFieldsExplicitly();
        
        descriptor.Field(t => t.Status);
        descriptor.Field(t => t.Priority);
        descriptor.Field(t => t.AssigneeId);
        descriptor.Field(t => t.DueDate);
        descriptor.Field(t => t.CreatedAt);
    }
}
