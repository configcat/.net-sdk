using System.Threading;
using System.Threading.Tasks;

namespace ConfigCat.HostingIntegration;

public interface IConfigCatInitializer
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
}
