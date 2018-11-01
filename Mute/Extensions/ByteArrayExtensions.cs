using System.Text;
using JetBrains.Annotations;

namespace Mute.Extensions
{
    public static class ByteArrayExtensions
    {
        public static string SHA256([NotNull] this byte[] data)
        {
            var result = new StringBuilder();

            using (var hash = System.Security.Cryptography.SHA256.Create())
            {
                var bytes = hash.ComputeHash(data);

                foreach (var b in bytes)
                    result.Append(b.ToString("x2"));
            }

            return result.ToString();
        }
    }
}
