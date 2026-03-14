# Granit.Validation.NorthAmerica

FluentValidation extension methods for North American identifiers and addresses (United States, Canada).

## Validators

### United States

| Extension | Description |
| --------- | ----------- |
| `SocialSecurityNumber()` | SSN (AAA-GG-SSSS), validates area/group/serial rules |
| `Ein()` | Employer Identification Number, validates IRS campus prefix |
| `UsStateCode()` | USPS 2-letter state/territory code (50 states + DC + territories) |
| `UsZipCode()` | ZIP code (5-digit or ZIP+4) |
| `NanpPhoneNumber()` | NANP phone (NXX-NXX-XXXX), various formats accepted |

### Canada

| Extension | Description |
| --------- | ----------- |
| `SocialInsuranceNumber()` | SIN (9 digits, Luhn check) |
| `CanadianPostalCode()` | Postal code (A1A 1A1) |
| `CanadianBusinessNumber()` | BN/NE (9 digits, Luhn check) |

## Usage

```csharp
using Granit.Validation.NorthAmerica.Extensions;

public class AddressValidator : AbstractValidator<Address>
{
    public AddressValidator()
    {
        RuleFor(x => x.Ssn).SocialSecurityNumber();
        RuleFor(x => x.ZipCode).UsZipCode();
        RuleFor(x => x.State).UsStateCode();
        RuleFor(x => x.Phone).NanpPhoneNumber();
    }
}
```
