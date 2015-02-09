using System;

namespace YoloDev.Xunit.AppVeyor
{
    internal static class Extensions
    {
        public static T Get<T>(this IServiceProvider provider)
        {
            return (T)provider.GetService(typeof(T));
        }
    }
}