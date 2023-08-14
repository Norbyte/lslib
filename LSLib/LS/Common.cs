using System;
using System.Text.RegularExpressions;

namespace LSLib.LS
{
	public static class Common
	{
		public const int MajorVersion = 1;

		public const int MinorVersion = 18;

		public const int PatchVersion = 5;

		// Version of LSTools profile data in generated DAE files
        public const int ColladaMetadataVersion = 3;

        /// <summary>
        /// Returns the version number of the LSLib library
        /// </summary>
        public static string LibraryVersion()
		{
			return String.Format("{0}.{1}.{2}", MajorVersion, MinorVersion, PatchVersion);
		}

		/// <summary>
		/// Compares the string against a given pattern.
		/// </summary>
		/// <param name="str">The string</param>
		/// <param name="pattern">The pattern to match, where "*" means any sequence of characters, and "?" means any single character</param>
		/// <returns><c>true</c> if the string matches the given pattern; otherwise <c>false</c>.</returns>
		public static bool Like(this string str, string pattern)
		{
			return new Regex("^" + Regex.Escape(pattern).Replace(@"\*", ".*").Replace(@"\?", ".") + "$", RegexOptions.Singleline).IsMatch(str);
		}

		/// <summary>
		/// Compares the string against a given pattern.
		/// </summary>
		/// <param name="str">The string</param>
		/// <param name="pattern">The pattern to match as a RegEx object</param>
		/// <returns><c>true</c> if the string matches the given pattern; otherwise <c>false</c>.</returns>
		public static bool Like(this string str, Regex pattern)
		{
			return pattern.IsMatch(str);
		}
	}
}
