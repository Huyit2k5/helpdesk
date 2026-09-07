using Helpdesk.Samples;
using Xunit;

namespace Helpdesk.EntityFrameworkCore.Domains;

[Collection(HelpdeskTestConsts.CollectionDefinitionName)]
public class EfCoreSampleDomainTests : SampleDomainTests<HelpdeskEntityFrameworkCoreTestModule>
{

}
