using Xunit;

namespace Helpdesk.EntityFrameworkCore;

[CollectionDefinition(HelpdeskTestConsts.CollectionDefinitionName)]
public class HelpdeskEntityFrameworkCoreCollection : ICollectionFixture<HelpdeskEntityFrameworkCoreFixture>
{

}
