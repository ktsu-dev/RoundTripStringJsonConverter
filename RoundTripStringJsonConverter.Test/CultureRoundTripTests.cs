// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.RoundTripStringJsonConverter.Tests;

using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
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

	/// <summary>
	/// Parses with a format provider but is not <see cref="IFormattable"/>, so the only way to write it
	/// is its culture-sensitive ToString().
	/// </summary>
	public readonly record struct Temperature(double Celsius)
	{
		public static Temperature Parse(string s, IFormatProvider? provider) => new(double.Parse(s, provider));

		public override string ToString() => Celsius.ToString(CultureInfo.CurrentCulture);
	}

	/// <summary>
	/// Has both a current-culture <c>Parse(string)</c> and an invariant-capable <c>Parse(string, IFormatProvider)</c>,
	/// like <see cref="BigInteger"/>, so it should be written and read with the invariant culture.
	/// </summary>
	public sealed record DualAmount(decimal Amount) : IFormattable
	{
		public static DualAmount Parse(string s) => new(decimal.Parse(s, CultureInfo.CurrentCulture));

		public static DualAmount Parse(string s, IFormatProvider? provider) => new(decimal.Parse(s, provider));

		public override string ToString() => Amount.ToString(CultureInfo.CurrentCulture);

		public string ToString(string? format, IFormatProvider? formatProvider) => Amount.ToString(format, formatProvider);
	}

	public class BigIntegerHolder
	{
		public BigInteger B { get; set; }
	}

	public class TemperatureHolder
	{
		public Temperature T { get; set; }
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
	public void NonFormattableFormatProviderParseTypeShouldRoundTripUnderACommaDecimalCulture()
	{
		JsonSerializerOptions options = GetOptions();

		string json = JsonSerializer.Serialize(new TemperatureHolder { T = new Temperature(1.5) }, options);
		TemperatureHolder? result = JsonSerializer.Deserialize<TemperatureHolder>(json, options);

		Assert.AreEqual("{\"T\":\"1.5\"}", json);
		Assert.AreEqual(new Temperature(1.5), result?.T);
	}

	[TestMethod]
	public void NonFormattableFormatProviderParseTypeShouldRoundTripAsADictionaryKeyUnderACommaDecimalCulture()
	{
		JsonSerializerOptions options = GetOptions();
		Dictionary<Temperature, int> original = new() { [new Temperature(2.5)] = 1 };

		string json = JsonSerializer.Serialize(original, options);
		Dictionary<Temperature, int>? result = JsonSerializer.Deserialize<Dictionary<Temperature, int>>(json, options);

		Assert.AreEqual("{\"2.5\":1}", json);
		Assert.IsNotNull(result);
		Assert.IsTrue(result.ContainsKey(new Temperature(2.5)));
	}

	[TestMethod]
	public void NonFormattableWriteShouldRestoreTheCurrentCulture()
	{
		JsonSerializerOptions options = GetOptions();
		CultureInfo before = CultureInfo.CurrentCulture;

		_ = JsonSerializer.Serialize(new Temperature(1.5), options);

		Assert.AreSame(before, CultureInfo.CurrentCulture);
	}

	[TestMethod]
	[DoNotParallelize]
	public void NonFormattableWriteShouldNotPinTheCurrentCulture()
	{
		JsonSerializerOptions options = GetOptions();
		CultureInfo? originalDefault = CultureInfo.DefaultThreadCurrentCulture;
		// Globalization-invariant builds can't load named cultures, so the default is a distinct clone
		CultureInfo laterDefault = (CultureInfo)CultureInfo.InvariantCulture.Clone();
		laterDefault.NumberFormat.NumberDecimalSeparator = ",";
		string? json = null;
		CultureInfo? afterOnThisThread = null;
		CultureInfo? afterOnANewThread = null;
		CultureInfo? afterInATask = null;

		void SerializeThenChangeTheDefault()
		{
			json = JsonSerializer.Serialize(new Temperature(1.5), options);
			CultureInfo.DefaultThreadCurrentCulture = laterDefault;
			afterOnThisThread = CultureInfo.CurrentCulture;

			Thread child = new(() => afterOnANewThread = CultureInfo.CurrentCulture);
			child.Start();
			child.Join();

			afterInATask = Task.Run(() => CultureInfo.CurrentCulture).GetAwaiter().GetResult();
		}

		try
		{
			// A thread with no inherited context has never set a culture, so it reads the default live
			Thread thread;
			using (ExecutionContext.SuppressFlow())
			{
				thread = new(SerializeThenChangeTheDefault);
				thread.Start();
			}

			thread.Join();
		}
		finally
		{
			CultureInfo.DefaultThreadCurrentCulture = originalDefault;
		}

		Assert.AreEqual("\"1.5\"", json);
		Assert.AreSame(laterDefault, afterOnThisThread, "The calling thread kept the culture the write pinned");
		Assert.AreSame(laterDefault, afterOnANewThread, "A thread started after the write inherited the pinned culture");
		Assert.AreSame(laterDefault, afterInATask, "A task started after the write inherited the pinned culture");
	}

	[TestMethod]
	public void NonFormattableWriteShouldKeepAnExplicitlySetCulture()
	{
		JsonSerializerOptions options = GetOptions();
		CultureInfo original = CultureInfo.CurrentCulture;
		CultureInfo explicitCulture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
		CultureInfo.CurrentCulture = explicitCulture;
		try
		{
			string json = JsonSerializer.Serialize(new Temperature(1.5), options);

			Assert.AreEqual("\"1.5\"", json);
			Assert.AreSame(explicitCulture, CultureInfo.CurrentCulture);
		}
		finally
		{
			CultureInfo.CurrentCulture = original;
		}
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

	/// <summary>
	/// A culture whose negative sign is U+2212 MINUS SIGN, as sv-SE, fi-FI and nb-NO use.
	/// </summary>
	private static CultureInfo MinusSignCulture()
	{
		CultureInfo culture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
		culture.NumberFormat.NegativeSign = "\u2212";
		return culture;
	}

	[TestMethod]
	public void TypeWithBothParseOverloadsShouldBeWrittenWithTheInvariantCulture()
	{
		JsonSerializerOptions options = GetOptions();

		string json = JsonSerializer.Serialize(new DualAmount(1.5m), options);
		DualAmount? result = JsonSerializer.Deserialize<DualAmount>(json, options);

		Assert.AreEqual("\"1.5\"", json);
		Assert.AreEqual(new DualAmount(1.5m), result);
	}

	[TestMethod]
	public void TypeWithBothParseOverloadsShouldRoundTripAsADictionaryKeyWithTheInvariantCulture()
	{
		JsonSerializerOptions options = GetOptions();
		Dictionary<DualAmount, int> original = new() { [new DualAmount(2.25m)] = 1 };

		string json = JsonSerializer.Serialize(original, options);
		Dictionary<DualAmount, int>? result = JsonSerializer.Deserialize<Dictionary<DualAmount, int>>(json, options);

		Assert.AreEqual("{\"2.25\":1}", json);
		Assert.IsNotNull(result);
		Assert.IsTrue(result.ContainsKey(new DualAmount(2.25m)));
	}

	[TestMethod]
	public void BigIntegerWrittenUnderAMinusSignCultureShouldReadUnderAnotherCulture()
	{
		JsonSerializerOptions options = GetOptions();

		CultureInfo.CurrentCulture = MinusSignCulture();
		string json = JsonSerializer.Serialize(new BigIntegerHolder { B = new BigInteger(-5) }, options);

		CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
		BigIntegerHolder? result = JsonSerializer.Deserialize<BigIntegerHolder>(json, options);

		Assert.AreEqual("{\"B\":\"-5\"}", json);
		Assert.AreEqual(new BigInteger(-5), result?.B);
	}

	[TestMethod]
	public void BigIntegerSavedInTheCurrentCultureBeforeTheFixShouldStillLoadInThatCulture()
	{
		JsonSerializerOptions options = GetOptions();
		CultureInfo.CurrentCulture = MinusSignCulture();

		BigIntegerHolder? result = JsonSerializer.Deserialize<BigIntegerHolder>("{\"B\":\"\u2212" + "5\"}", options);

		Assert.AreEqual(new BigInteger(-5), result?.B);
	}
}
