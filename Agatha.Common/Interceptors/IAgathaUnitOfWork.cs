using System;

namespace Agatha.Common.Interceptors
{
    public interface IAgathaUnitOfWork
    {
        void Start();

        void End(Exception ex = null);
    }
}
