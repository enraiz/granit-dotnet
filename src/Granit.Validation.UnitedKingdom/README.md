# Granit.Validation.UnitedKingdom

FluentValidation extension methods for United Kingdom identifiers, tax numbers, addresses, and payment codes.

## Validators

| Extension | Description |
| --------- | ----------- |
| `NationalInsuranceNumber()` | NI number (2 letters + 6 digits + suffix A–D), invalid prefix rejection |
| `NhsNumber()` | NHS number (10 digits, MOD 11 check digit) |
| `UniqueTaxpayerReference()` | UTR (10 digits, MOD 11 check digit) |
| `UkVat()` | UK VAT (GB + 9/12 digits, MOD 97 check), GD/HA prefixes |
| `CompaniesHouseNumber()` | Companies House (8 chars, optional 2-letter prefix) |
| `UkPostcode()` | UK postcode (all standard formats, case-insensitive) |
| `SortCode()` | Bank sort code (6 digits, XX-XX-XX) |

## Usage

```csharp
using Granit.Validation.UnitedKingdom.Extensions;

public class UkCustomerValidator : AbstractValidator<UkCustomer>
{
    public UkCustomerValidator()
    {
        RuleFor(x => x.NiNumber).NationalInsuranceNumber();
        RuleFor(x => x.Postcode).UkPostcode();
        RuleFor(x => x.VatNumber).UkVat();
        RuleFor(x => x.SortCode).SortCode();
    }
}
```
