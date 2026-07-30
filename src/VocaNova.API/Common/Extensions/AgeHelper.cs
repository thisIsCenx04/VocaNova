namespace VocaNova.API.Common.Extensions;

public static class AgeHelper
{
    /// <summary>
    /// Calculates the completed years between <paramref name="dateOfBirth"/> and <paramref name="asOf"/>.
    /// Returns a negative value when the date of birth is in the future.
    /// </summary>
    public static int CalculateAge(DateOnly dateOfBirth, DateOnly asOf)
    {
        var age = asOf.Year - dateOfBirth.Year;
        if (dateOfBirth > asOf.AddYears(-age))
        {
            age--;
        }

        return age;
    }

    public static int CalculateAge(DateOnly dateOfBirth)
    {
        return CalculateAge(dateOfBirth, DateOnly.FromDateTime(DateTime.UtcNow));
    }
}
