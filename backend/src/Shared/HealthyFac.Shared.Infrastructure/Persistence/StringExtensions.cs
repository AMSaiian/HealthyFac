namespace HealthyFac.Shared.Infrastructure.Persistence;

public static class StringExtensions
{
    extension(string value)
    {
        /// <summary>
        /// Converts a Pascal case name to snake case, for example <c>SerialNumber</c> to <c>serial_number</c>.
        /// </summary>
        /// <returns>The lower case name with an underscore before each inner upper case letter.</returns>
        public string ToSnakeCase()
        {
            return string.Concat(
                    value.Select((character, index) => index > 0 && char.IsUpper(character)
                        ? $"_{character}"
                        : character.ToString()))
                .ToLowerInvariant();
        }
    }
}
