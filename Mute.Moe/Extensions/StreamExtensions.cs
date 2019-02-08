using System.IO;
using System.Text;
using JetBrains.Annotations;

namespace Mute.Moe.Extensions
{
    public static class StreamExtensions
    {
        public static string SHA256([NotNull] this Stream stream)
        {
            var result = new StringBuilder();

            using (var hash = System.Security.Cryptography.SHA256.Create())
            {
                stream.Position = 0;
                var bytes = hash.ComputeHash(stream);
                stream.Position = 0;

                foreach (var b in bytes)
                    result.Append(b.ToString("x2"));
            }

            return result.ToString();
        }
    }
}
