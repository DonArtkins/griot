using Griot.Api.GraphQL.Types;
using HotChocolate.Data.Filters;

namespace Griot.Api.GraphQL.Filters;

public class NotificationFilterInputType : FilterInputType<NotificationGraphQLType>
{
    protected override void Configure(IFilterInputTypeDescriptor<NotificationGraphQLType> descriptor)
    {
        descriptor.BindFieldsExplicitly();
        
        descriptor.Field(n => n.ReadAt);
        descriptor.Field(n => n.Type);
        descriptor.Field(n => n.CreatedAt);
    }
}
