using System;

namespace CloudNimble.DotNetDocs.Tools.Models
{

    /// <summary>
    /// Represents a simplified NuGet version for comparison purposes.
    /// </summary>
    /// <remarks>
    /// This class provides basic semantic versioning support with major, minor, patch, and prerelease components.
    /// It implements <see cref="IComparable{T}"/> to allow version comparisons, where stable versions are considered
    /// greater than prerelease versions with the same major.minor.patch numbers.
    /// </remarks>
    public class NuGetVersion : IComparable<NuGetVersion>
    {

        #region Properties

        /// <summary>
        /// Gets a value indicating whether this version is a prerelease version.
        /// </summary>
        public bool IsPrerelease => !string.IsNullOrEmpty(Prerelease);

        /// <summary>
        /// Gets the major version component.
        /// </summary>
        public int Major { get; }

        /// <summary>
        /// Gets the minor version component.
        /// </summary>
        public int Minor { get; }

        /// <summary>
        /// Gets the patch version component.
        /// </summary>
        public int Patch { get; }

        /// <summary>
        /// Gets the prerelease label, if any.
        /// </summary>
        public string? Prerelease { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="NuGetVersion"/> class from a version string.
        /// </summary>
        /// <param name="version">The version string to parse (e.g., "1.0.0" or "1.0.0-preview.1").</param>
        /// <remarks>
        /// The version string should be in the format "major.minor.patch" or "major.minor.patch-prerelease".
        /// If any component cannot be parsed, it defaults to 0.
        /// </remarks>
        public NuGetVersion(string version)
        {
            var parts = version.Split('-', 2);
            var versionPart = parts[0];
            Prerelease = parts.Length > 1 ? parts[1] : null;

            var numbers = versionPart.Split('.');
            Major = numbers.Length > 0 && int.TryParse(numbers[0], out var major) ? major : 0;
            Minor = numbers.Length > 1 && int.TryParse(numbers[1], out var minor) ? minor : 0;
            Patch = numbers.Length > 2 && int.TryParse(numbers[2], out var patch) ? patch : 0;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Compares the current version to another version.
        /// </summary>
        /// <param name="other">The version to compare to.</param>
        /// <returns>A negative value if this version is less than <paramref name="other"/>, zero if they are equal,
        /// or a positive value if this version is greater than <paramref name="other"/>.</returns>
        /// <remarks>
        /// Version comparison follows semantic versioning rules. Stable versions are considered greater than
        /// prerelease versions with the same major.minor.patch numbers. Prerelease versions are compared
        /// lexicographically by their prerelease labels.
        /// </remarks>
        public int CompareTo(NuGetVersion? other)
        {
            if (other is null) return 1;

            // Compare major, minor, patch
            var result = Major.CompareTo(other.Major);
            if (result != 0) return result;

            result = Minor.CompareTo(other.Minor);
            if (result != 0) return result;

            result = Patch.CompareTo(other.Patch);
            if (result != 0) return result;

            // Stable versions are greater than prerelease versions
            if (!IsPrerelease && other.IsPrerelease) return 1;
            if (IsPrerelease && !other.IsPrerelease) return -1;

            // Compare prerelease strings (simple string comparison)
            return string.Compare(Prerelease, other.Prerelease, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current version.
        /// </summary>
        /// <param name="obj">The object to compare with the current version.</param>
        /// <returns><c>true</c> if the specified object is equal to the current version; otherwise, <c>false</c>.</returns>
        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj is null)
            {
                return false;
            }

            return obj is NuGetVersion other && CompareTo(other) == 0;
        }

        /// <summary>
        /// Returns a hash code for the current version.
        /// </summary>
        /// <returns>A hash code for the current version.</returns>
        public override int GetHashCode()
        {
            return HashCode.Combine(Major, Minor, Patch, Prerelease);
        }

        /// <summary>
        /// Returns a string representation of the version.
        /// </summary>
        /// <returns>A string in the format "major.minor.patch" or "major.minor.patch-prerelease".</returns>
        public override string ToString()
        {
            var version = $"{Major}.{Minor}.{Patch}";

            return IsPrerelease ? $"{version}-{Prerelease}" : version;
        }

        #endregion

        #region Operators

        /// <summary>
        /// Determines whether two versions are equal.
        /// </summary>
        /// <param name="left">The first version to compare.</param>
        /// <param name="right">The second version to compare.</param>
        /// <returns><c>true</c> if the versions are equal; otherwise, <c>false</c>.</returns>
        public static bool operator ==(NuGetVersion? left, NuGetVersion? right)
        {
            if (left is null)
            {
                return right is null;
            }

            return left.Equals(right);
        }

        /// <summary>
        /// Determines whether two versions are not equal.
        /// </summary>
        /// <param name="left">The first version to compare.</param>
        /// <param name="right">The second version to compare.</param>
        /// <returns><c>true</c> if the versions are not equal; otherwise, <c>false</c>.</returns>
        public static bool operator !=(NuGetVersion? left, NuGetVersion? right)
        {
            return !(left == right);
        }

        /// <summary>
        /// Determines whether the first version is greater than or equal to the second version.
        /// </summary>
        /// <param name="left">The first version to compare.</param>
        /// <param name="right">The second version to compare.</param>
        /// <returns><c>true</c> if the first version is greater than or equal to the second version; otherwise, <c>false</c>.</returns>
        public static bool operator >=(NuGetVersion? left, NuGetVersion? right)
        {
            if (left is null)
            {
                return right is null;
            }

            return left.CompareTo(right) >= 0;
        }

        /// <summary>
        /// Determines whether the first version is greater than the second version.
        /// </summary>
        /// <param name="left">The first version to compare.</param>
        /// <param name="right">The second version to compare.</param>
        /// <returns><c>true</c> if the first version is greater than the second version; otherwise, <c>false</c>.</returns>
        public static bool operator >(NuGetVersion? left, NuGetVersion? right)
        {
            if (left is null)
            {
                return false;
            }

            return left.CompareTo(right) > 0;
        }

        /// <summary>
        /// Determines whether the first version is less than or equal to the second version.
        /// </summary>
        /// <param name="left">The first version to compare.</param>
        /// <param name="right">The second version to compare.</param>
        /// <returns><c>true</c> if the first version is less than or equal to the second version; otherwise, <c>false</c>.</returns>
        public static bool operator <=(NuGetVersion? left, NuGetVersion? right)
        {
            if (left is null)
            {
                return true;
            }

            return left.CompareTo(right) <= 0;
        }

        /// <summary>
        /// Determines whether the first version is less than the second version.
        /// </summary>
        /// <param name="left">The first version to compare.</param>
        /// <param name="right">The second version to compare.</param>
        /// <returns><c>true</c> if the first version is less than the second version; otherwise, <c>false</c>.</returns>
        public static bool operator <(NuGetVersion? left, NuGetVersion? right)
        {
            if (left is null)
            {
                return right is not null;
            }

            return left.CompareTo(right) < 0;
        }

        #endregion

    }

}
