using JetBrains.Annotations;
using Ninject;

namespace Mute.Extensions
{
    public static class StandardKernelExtensions
    {
        [NotNull] public static StandardKernel AddSingleton<T>([NotNull] this StandardKernel kernel)
        {
            kernel.Bind<T>().To<T>().InSingletonScope();
            return kernel;
        }
    }
}
