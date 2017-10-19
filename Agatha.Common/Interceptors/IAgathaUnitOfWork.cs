using System;
using System.Threading.Tasks;

namespace Agatha.Common.Interceptors
{
    public interface IAgathaUnitOfWork
    {
        Task Start();

        Task End(Exception ex = null);
    }
}
