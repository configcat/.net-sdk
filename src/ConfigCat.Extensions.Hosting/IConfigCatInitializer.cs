using System.Threading;
using System.Threading.Tasks;

namespace ConfigCat.Extensions.Hosting;

public interface IConfigCatInitializer
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
}
