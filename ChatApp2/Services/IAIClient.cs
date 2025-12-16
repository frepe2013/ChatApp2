using System.Threading;
using System.Threading.Tasks;

namespace ChatApp2.Services
{
    public interface IAIClient
    {
        Task<string?> AskAsync(string? question, CancellationToken cancellationToken = default);
    }
}