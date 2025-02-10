using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureEventHubsEventProcessor
{
    public record EventPayload(Guid Id, string LocationId, string Content);
}
