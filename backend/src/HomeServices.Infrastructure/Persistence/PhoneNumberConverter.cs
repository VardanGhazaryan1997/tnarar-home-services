using HomeServices.Domain.Identity;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace HomeServices.Infrastructure.Persistence;

/// <summary>Stores <see cref="PhoneNumber"/> as its E.164 string.</summary>
public sealed class PhoneNumberConverter() : ValueConverter<PhoneNumber, string>(
    phone => phone.Value,
    value => PhoneNumber.Parse(value));
