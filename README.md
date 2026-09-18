# MobileNumberGenerator

Generates random phone numbers that pass Google's [libphonenumber](https://github.com/google/libphonenumber) validation as a **mobile** number for a given country — useful for seeding test data, fixtures, demos, or QA environments without hand-maintaining number formats per country.

Unlike libphonenumber's own `GetExampleNumberForType`, which always returns the *same* fixed example number for a region, this library samples random candidates from that region's actual mobile numbering pattern (via [Fare](https://github.com/moodmosaic/Fare)/Xeger) and validates each one with libphonenumber before returning it — so you get a different, genuinely valid-format number every call.

## Install

```
dotnet add package Panagis.MobileNumberGenerator
```

## Usage

```csharp
using Panagis.MobileNumberGenerator;
using PhoneNumbers;

string number = PhoneNumberGenerator.Generate("GR");
// e.g. "+306945178203"

string usNumber = PhoneNumberGenerator.Generate("US", PhoneNumberFormat.NATIONAL);
// e.g. "(212) 555-0148"
```

You can also get the parsed `PhoneNumber` object directly if you want to format or inspect it yourself:

```csharp
PhoneNumber number = PhoneNumberGenerator.GeneratePhoneNumber("GB");
```

## How it works

1. Looks up the region's mobile number pattern from libphonenumber's metadata.
2. Uses `Xeger` to generate random strings that match that pattern.
3. Parses and validates each candidate with libphonenumber, checking both `IsValidNumber` and that the number type is classified as `MOBILE`.
4. Returns the first candidate that passes (tries up to `maxAttempts`, default 30).
5. If no candidate validates, falls back to that region's official libphonenumber example number — so you always get *something* valid rather than an exception, unless the region code itself is invalid.

An unrecognized region code throws an `ArgumentException` immediately, rather than silently falling back to a number from the wrong country.

## ⚠️ Test-data safety

Passing libphonenumber validation only means a number is *plausible* under a country's numbering plan — it does not mean the number is unused or reserved for testing. A generated number may belong to a real subscriber.

**Do not** call, text, or message a generated number, and don't use it in any test that will actually attempt delivery to it. Use it for things like UI rendering, format validation, database seeding, and similar offline purposes only.

## License

MIT — see [LICENSE](LICENSE).
