using System;
using System.Security.Cryptography;

namespace Mute.Moe.Services.Randomness
{
    public class CryptoDiceRoller
        : IDiceRoller
    {
        private readonly RNGCryptoServiceProvider _rng;

        public CryptoDiceRoller()
        {
            _rng = new RNGCryptoServiceProvider();
        }

        public ulong Roll(ulong sides)
        {
            if (sides == 0)
                return 0;

            // How many full sets of `sides` can we fit into `MaxValue`. If our value was 1 byte long that means we'll always a pick a random number between 0-255, if we're rolling a D100 that means
            // that the range is not complete. 0->99 is ok. 100->199 is also ok (wrap around). 200->255 is useless, if we return that the distribution of picked values will not be uniform.
            var fullSetsOfValues = ulong.MaxValue / sides;

            //Keep re-rolling until the roll is fair
            ulong randomNumber = 0;
            do
            {
                unsafe
                {
                    _rng.GetBytes(new Span<byte>(&randomNumber, sizeof(ulong)));
                }
            }
            while (randomNumber >= sides * fullSetsOfValues);

            //Now that we have a fair value, return it within the specified range
            return (randomNumber % sides) + 1;
        }
    }
}
