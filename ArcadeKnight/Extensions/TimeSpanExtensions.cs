using System;

namespace ArcadeKnight.Extensions;

internal static class TimeSpanExtensions
{
	#region Methods

	internal static string ToFormat(this TimeSpan value, string format) 
		=> new DateTime(value.Ticks).ToString(format);

	#endregion
}
