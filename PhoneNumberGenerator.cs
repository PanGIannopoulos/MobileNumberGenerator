using System;
using Fare;
using PhoneNumbers;

namespace Panagis.MobileNumberGenerator
{
    /// <summary>
    /// Generates random phone numbers that pass Google libphonenumber's
    /// validation as a <see cref="PhoneNumberType.MOBILE"/> number for a
    /// given ISO 3166-1 alpha-2 region.
    /// </summary>
    public static class PhoneNumberGenerator
    {
        private const int DefaultMaxAttempts = 30;

        private static readonly PhoneNumberUtil PhoneUtil = PhoneNumberUtil.GetInstance();

        // A single shared Random instance, guarded by a lock, rather than a new
        // Random() per call. This isn't just a style choice: Fare's Xeger has a
        // known issue where rapid, back-to-back "new Random()" instances (each
        // seeded from the system clock) can produce identical sequences, causing
        // the same "random" candidate to come out repeatedly - see
        // https://github.com/moodmosaic/Fare/issues/26. Sharing one instance
        // avoids that failure mode entirely, on every target framework.
        private static readonly Random SharedRandom = new Random();
        private static readonly object RandomLock = new object();

        /// <summary>
        /// Generates a random phone number for <paramref name="regionCode"/>,
        /// formatted according to <paramref name="format"/>. The result passes
        /// libphonenumber's <c>IsValidNumber</c> check and is classified as
        /// <see cref="PhoneNumberType.MOBILE"/>.
        /// </summary>
        /// <param name="regionCode">ISO 3166-1 alpha-2 region code, e.g. "GR", "US", "GB".</param>
        /// <param name="format">Output format. Defaults to E.164 (e.g. "+306912345678").</param>
        /// <param name="maxAttempts">
        /// How many random candidates to try against the region's mobile pattern
        /// before falling back to that region's own libphonenumber example number.
        /// </param>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="regionCode"/> is null, empty, or not a
        /// region libphonenumber recognizes.
        /// </exception>
        public static string Generate(
            string regionCode,
            PhoneNumberFormat format = PhoneNumberFormat.E164,
            int maxAttempts = DefaultMaxAttempts)
        {
            PhoneNumber number = GeneratePhoneNumber(regionCode, maxAttempts);
            return PhoneUtil.Format(number, format);
        }

        /// <summary>
        /// Same as <see cref="Generate"/>, but returns the parsed <see cref="PhoneNumber"/>
        /// instead of a formatted string, in case you want to format or inspect it yourself.
        /// </summary>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="regionCode"/> is null, empty, or not a
        /// region libphonenumber recognizes.
        /// </exception>
        public static PhoneNumber GeneratePhoneNumber(string regionCode, int maxAttempts = DefaultMaxAttempts)
        {
            string normalizedRegion = ValidateAndNormalize(regionCode);

            return TryGenerateFromPattern(normalizedRegion, maxAttempts)
                   ?? GetExampleMobileNumber(normalizedRegion);
        }

        private static string ValidateAndNormalize(string regionCode)
        {
            if (string.IsNullOrWhiteSpace(regionCode))
                throw new ArgumentException("Region code must not be null or empty.", nameof(regionCode));

            string normalized = regionCode.Trim().ToUpperInvariant();

            if (!PhoneUtil.GetSupportedRegions().Contains(normalized))
                throw new ArgumentException(
                    $"'{regionCode}' is not a region code libphonenumber recognizes.",
                    nameof(regionCode));

            return normalized;
        }

        private static PhoneNumber? TryGenerateFromPattern(string regionCode, int maxAttempts)
        {
            PhoneMetadata metadata = PhoneUtil.GetMetadataForRegion(regionCode);
            string? pattern = metadata?.Mobile?.NationalNumberPattern;

            if (string.IsNullOrEmpty(pattern))
                return null;

            lock (RandomLock)
            {
                var xeger = new Xeger(pattern, SharedRandom);

                for (var attempt = 0; attempt < maxAttempts; attempt++)
                {
                    string candidate = xeger.Generate();

                    try
                    {
                        PhoneNumber parsed = PhoneUtil.Parse(candidate, regionCode);

                        if (PhoneUtil.IsValidNumber(parsed) &&
                            PhoneUtil.GetNumberType(parsed) == PhoneNumberType.MOBILE)
                        {
                            return parsed;
                        }
                    }
                    catch (NumberParseException)
                    {
                        // Candidate wasn't parseable as this region - try another.
                    }
                }
            }

            return null;
        }

        private static PhoneNumber GetExampleMobileNumber(string regionCode)
        {
            return PhoneUtil.GetExampleNumberForType(regionCode, PhoneNumberType.MOBILE)
                   ?? throw new InvalidOperationException(
                       $"libphonenumber has no MOBILE example number for region '{regionCode}'.");
        }
    }
}
