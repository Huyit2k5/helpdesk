using Helpdesk.Samples;
using Xunit;

namespace Helpdesk.EntityFrameworkCore.Applications;

[Collection(HelpdeskTestConsts.CollectionDefinitionName)]
public class EfCoreSampleAppServiceTests : SampleAppServiceTests<HelpdeskEntityFrameworkCoreTestModule>
{

}
