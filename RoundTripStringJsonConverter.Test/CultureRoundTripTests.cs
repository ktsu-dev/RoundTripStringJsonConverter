// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.RoundTripStringJsonConverter.Tests;

using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Types read through <c>Parse(string, IFormatProvider)</c> are parsed with the invariant culture, so they
/// must also be written with it, or a machine whose decimal separator is ',' corrupts them silently.
/// </summary>
[TestClass]
public class CultureRoundTripTests
{
	public sealed record Money(decimal Amount) : IFormattable
	{
		public static Money Parse(string s, IFormatProvider? provider) => new(decimal.Parse(s, provider));

		public override string ToString() => Amount.ToString(CultureInfo.CurrentCulture);

		public string ToString(string? format, IFormatProvider? formatProvider) => Amount.ToString(format, formatProvider);
	}

	/// <summary>
	/// Parses with the current culture, so its culture-sensitive ToString() must be left alone.
	/// </summary>
	public sealed record LocalAmount(decimal Amount) : IFormattable
	{
		public static LocalAmount Parse(string s) => new(decimal.Parse(s, CultureInfo.CurrentCulture));

		public override string ToString() => Amount.ToString(CultureInfo.CurrentCulture);

		public string ToString(string? format, IFormatProvider? formatProvider) => Amount.ToString(format, formatProvider);
	}

	public class ComplexHolder
	{
		public Complex C { get; set; }
	}

	public class MoneyHolder
	{
		public Money? M { get; set; }
	}

	private CultureInfo? originalCulture;

	[TestInitialize]
	public void UseCommaDecimalCulture()
	{
		originalCulture = CultureInfo.CurrentCulture;

		// The test project runs with invariant globalization, where named cultures such as de-DE can't be
		// created, so build one with the same decimal and group separators instead.
		CultureInfo commaDecimal = (CultureInfo)CultureInfo.InvariantCulture.Clone();
		commaDecimal.NumberFormat.NumberDecimalSeparator = ",";
		commaDecimal.NumberFormat.NumberGroupSeparator = ".";
		CultureInfo.CurrentCulture = commaDecimal;
	}

	[TestCleanup]
	public void RestoreCulture() => CultureInfo.CurrentCulture = originalCulture!;

	private static JsonSerializerOptions GetOptions()
	{
		JsonSerializerOptions options = new();
		options.Converters.Add(new RoundTripStringJsonConverterFactory());
		return options;
	}

	[TestMethod]
	public void ComplexShouldRoundTripUnderACommaDecimalCulture()
	{
		JsonSerializerOptions options = GetOptions();

		string json = JsonSerializer.Serialize(new ComplexHolder { C = new Complex(1.5, 2.5) }, options);
		ComplexHolder? result = JsonSerializer.Deserialize<ComplexHolder>(json, options);

		// The default encoder escapes '<' and '>', so check the numbers rather than the whole string.
		StringAssert.Contains(json, "1.5; 2.5");
		Assert.IsNotNull(result);
		Assert.AreEqual(new Complex(1.5, 2.5), result.C);
	}

	[TestMethod]
	public void FormatProviderParseTypeShouldRoundTripUnderACommaDecimalCulture()
	{
		JsonSerializerOptions options = GetOptions();

		string json = JsonSerializer.Serialize(new MoneyHolder { M = new Money(1.5m) }, options);
		MoneyHolder? result = JsonSerializer.Deserialize<MoneyHolder>(json, options);

		Assert.AreEqual("{\"M\":\"1.5\"}", json);
		Assert.AreEqual(new Money(1.5m), result?.M);
	}

	[TestMethod]
	public void FormatProviderParseTypeShouldRoundTripAsADictionaryKeyUnderACommaDecimalCulture()
	{
		JsonSerializerOptions options = GetOptions();
		Dictionary<Money, int> original = new() { [new Money(2.25m)] = 1 };

		string json = JsonSerializer.Serialize(original, options);
		Dictionary<Money, int>? result = JsonSerializer.Deserialize<Dictionary<Money, int>>(json, options);

		Assert.AreEqual("{\"2.25\":1}", json);
		Assert.IsNotNull(result);
		Assert.IsTrue(result.ContainsKey(new Money(2.25m)));
	}

	[TestMethod]
	public void CurrentCultureParseTypeShouldKeepItsOwnToString()
	{
		JsonSerializerOptions options = GetOptions();

		string json = JsonSerializer.Serialize(new LocalAmount(1.5m), options);
		LocalAmount? result = JsonSerializer.Deserialize<LocalAmount>(json, options);

		Assert.AreEqual("\"1,5\"", json);
		Assert.AreEqual(new LocalAmount(1.5m), result);
	}
}
