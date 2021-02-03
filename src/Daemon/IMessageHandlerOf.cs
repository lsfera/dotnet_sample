using System.Threading;
using System.Threading.Tasks;

namespace Daemon
{
    internal interface IMessageHandlerOf<in T> where T : class
    {
        Task HandleAsync(T message, CancellationToken token);
    }
}