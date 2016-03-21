using System;
using System.Collections.Generic;

namespace Nitrassic.Library
{
	/// <summary>
	/// The prototype for the Date object.
	/// </summary>
	public partial class Date
	{
		/// <summary>
		/// The underlying DateTime value.
		/// </summary>
		private DateTime value;
		
		/// <summary>
		/// A DateTime that represents an invalid date.
		/// </summary>
		private static readonly DateTime InvalidDate = DateTime.MinValue;



		//	 INITIALIZATION
		//_________________________________________________________________________________________
		
		/// <summary>
		/// Creates a new Date instance and initializes it to the current time.
		/// </summary>
		/// <param name="prototype"> The next object in the prototype chain. </param>
		public Date()
			: this(GetNow())
		{
		}

		/// <summary>
		/// Creates a new Date instance from the given date value.
		/// </summary>
		/// <param name="prototype"> The next object in the prototype chain. </param>
		/// <param name="value"> The number of milliseconds since January 1, 1970, 00:00:00 UTC. </param>
		public Date(double value)
			: this(ToDateTime(value >= 0 ? Math.Floor(value) : Math.Ceiling(value)))
		{
		}

		/// <summary>
		/// Creates a new Date instance from the given date string.
		/// </summary>
		/// <param name="prototype"> The next object in the prototype chain. </param>
		/// <param name="dateStr"> A string representing a date, expressed in RFC 1123 format. </param>
		public Date(string dateStr)
			: this(DateParser.Parse(dateStr))
		{
		}

		/// <summary>
		/// Creates a new Date instance from various date components, expressed in local time.
		/// </summary>
		/// <param name="prototype"> The next object in the prototype chain. </param>
		/// <param name="year"> The full year. </param>
		/// <param name="month"> The month as an integer between 0 and 11 (january to december). </param>
		/// <param name="day"> The day of the month, from 1 to 31.  Defaults to 1. </param>
		/// <param name="hour"> The number of hours since midnight, from 0 to 23.  Defaults to 0. </param>
		/// <param name="minute"> The number of minutes, from 0 to 59.  Defaults to 0. </param>
		/// <param name="second"> The number of seconds, from 0 to 59.  Defaults to 0. </param>
		/// <param name="millisecond"> The number of milliseconds, from 0 to 999.  Defaults to 0. </param>
		/// <remarks>
		/// If any of the parameters are out of range, then the other values are modified accordingly.
		/// </remarks>
		public Date(int year, int month, int day, int hour,
			int minute, int second, int millisecond)
			: this(Date.ToDateTime(year, month, day, hour, minute, second, millisecond, DateTimeKind.Local))
		{
		}

		/// <summary>
		/// Creates a new Date instance from the given date.
		/// </summary>
		/// <param name="prototype"> The next object in the prototype chain. </param>
		/// <param name="dateTime"> The date to set the instance value to. </param>
		private Date(DateTime dateTime)
		{
			this.value = dateTime;
		}



		//	 .NET ACCESSOR PROPERTIES
		//_________________________________________________________________________________________
		
		/// <summary>
		/// Gets the date represented by this object in standard .NET DateTime format.
		/// </summary>
		public DateTime Value
		{
			get { return this.value; }
		}

		/// <summary>
		/// Gets the date represented by this object as the number of milliseconds elapsed since
		/// January 1, 1970, 00:00:00 UTC.
		/// </summary>
		public double ValueInMilliseconds
		{
			get { return ToJSDate(this.value); }
		}

		/// <summary>
		/// Gets a value indicating whether the date instance is valid.  A date can be invalid if
		/// NaN is passed to any of the constructor parameters.
		/// </summary>
		public bool IsValid
		{
			get { return this.value == InvalidDate; }
		}
		
		//	 JAVASCRIPT FUNCTIONS
		//_________________________________________________________________________________________


		/// <summary>
		/// Returns the year component of this date, according to local time.
		/// </summary>
		/// <returns> The year component of this date, according to local time. </returns>
		public double GetFullYear()
		{
			return GetDateComponent(DateComponent.Year, DateTimeKind.Local);
		}
		
		/// <summary>
		/// Returns the month component of this date, according to local time.
		/// </summary>
		/// <returns> The month component (0-11) of this date, according to local time. </returns>
		public double GetMonth()
		{
			return GetDateComponent(DateComponent.Month, DateTimeKind.Local);
		}

		/// <summary>
		/// Returns the day of the month component of this date, according to local time.
		/// </summary>
		/// <returns> The day of the month component (1-31) of this date, according to local time. </returns>
		public double GetDate()
		{
			return GetDateComponent(DateComponent.Day, DateTimeKind.Local);
		}

		/// <summary>
		/// Returns the day of the week component of this date, according to local time.
		/// </summary>
		/// <returns> The day of the week component (0-6) of this date, according to local time. </returns>
		public double GetDay()
		{
			return GetDateComponent(DateComponent.DayOfWeek, DateTimeKind.Local);
		}

		/// <summary>
		/// Returns the hour component of this date, according to local time.
		/// </summary>
		/// <returns> The hour component (0-23) of this date, according to local time. </returns>
		public double GetHours()
		{
			return GetDateComponent(DateComponent.Hour, DateTimeKind.Local);
		}

		/// <summary>
		/// Returns the minute component of this date, according to local time.
		/// </summary>
		/// <returns> The minute component (0-59) of this date, according to local time. </returns>
		public double GetMinutes()
		{
			return GetDateComponent(DateComponent.Minute, DateTimeKind.Local);
		}

		/// <summary>
		/// Returns the seconds component of this date, according to local time.
		/// </summary>
		/// <returns> The seconds component (0-59) of this date, according to local time. </returns>
		public double GetSeconds()
		{
			return GetDateComponent(DateComponent.Second, DateTimeKind.Local);
		}

		/// <summary>
		/// Returns the millisecond component of this date, according to local time.
		/// </summary>
		/// <returns> The millisecond component (0-999) of this date, according to local time. </returns>
		public double GetMilliseconds()
		{
			return GetDateComponent(DateComponent.Millisecond, DateTimeKind.Local);
		}

		/// <summary>
		/// Returns the number of milliseconds since January 1, 1970, 00:00:00 UTC.
		/// </summary>
		/// <returns> The number of milliseconds since January 1, 1970, 00:00:00 UTC. </returns>
		public double GetTime()
		{
			return this.ValueInMilliseconds;
		}

		/// <summary>
		/// Returns the time-zone offset in minutes for the current locale.
		/// </summary>
		/// <returns> The time-zone offset in minutes for the current locale. </returns>
		public double GetTimezoneOffset()
		{
			if (this.value == InvalidDate)
				return double.NaN;
			return -(int)TimeZoneInfo.Local.GetUtcOffset(this.Value).TotalMinutes;
		}

		/// <summary>
		/// Returns the year component of this date, according to universal time.
		/// </summary>
		/// <returns> The year component of this date, according to universal time. </returns>
		public double GetUTCFullYear()
		{
			return GetDateComponent(DateComponent.Year, DateTimeKind.Utc);
		}

		/// <summary>
		/// Returns the month component of this date, according to universal time.
		/// </summary>
		/// <returns> The month component (0-11) of this date, according to universal time. </returns>
		public double GetUTCMonth()
		{
			return GetDateComponent(DateComponent.Month, DateTimeKind.Utc);
		}

		/// <summary>
		/// Returns the day of the month component of this date, according to universal time.
		/// </summary>
		/// <returns> The day of the month component (1-31) of this date, according to universal time. </returns>
		public double GetUTCDate()
		{
			return GetDateComponent(DateComponent.Day, DateTimeKind.Utc);
		}

		/// <summary>
		/// Returns the day of the week component of this date, according to universal time.
		/// </summary>
		/// <returns> The day of the week component (0-6) of this date, according to universal time. </returns>
		public double GetUTCDay()
		{
			return GetDateComponent(DateComponent.DayOfWeek, DateTimeKind.Utc);
		}

		/// <summary>
		/// Returns the hour component of this date, according to universal time.
		/// </summary>
		/// <returns> The hour component (0-23) of this date, according to universal time. </returns>
		public double GetUTCHours()
		{
			return GetDateComponent(DateComponent.Hour, DateTimeKind.Utc);
		}

		/// <summary>
		/// Returns the minute component of this date, according to universal time.
		/// </summary>
		/// <returns> The minute component (0-59) of this date, according to universal time. </returns>
		public double GetUTCMinutes()
		{
			return GetDateComponent(DateComponent.Minute, DateTimeKind.Utc);
		}

		/// <summary>
		/// Returns the seconds component of this date, according to universal time.
		/// </summary>
		/// <returns> The seconds component (0-59) of this date, according to universal time. </returns>
		public double GetUTCSeconds()
		{
			return GetDateComponent(DateComponent.Second, DateTimeKind.Utc);
		}

		/// <summary>
		/// Returns the millisecond component of this date, according to universal time.
		/// </summary>
		/// <returns> The millisecond component (0-999) of this date, according to universal time. </returns>
		public double GetUTCMilliseconds()
		{
			return GetDateComponent(DateComponent.Millisecond, DateTimeKind.Utc);
		}

		/// <summary>
		/// Sets the full year (4 digits for 4-digit years) of this date, according to local time.
		/// </summary>
		/// <param name="year"> The 4 digit year. </param>
		/// <returns> The number of milliseconds elapsed since January 1, 1970, 00:00:00 UTC for
		/// the new date. </returns>
		public double SetFullYear(double year)
		{
			return SetDateComponents(DateComponent.Year, DateTimeKind.Local, year);
		}

		/// <summary>
		/// Sets the full year (4 digits for 4-digit years) of this date, according to local time.
		/// </summary>
		/// <param name="year"> The 4 digit year. </param>
		/// <param name="month"> The month (0-11). </param>
		/// <returns> The number of milliseconds elapsed since January 1, 1970, 00:00:00 UTC for
		/// the new date. </returns>
		public double SetFullYear(double year, double month)
		{
			return SetDateComponents(DateComponent.Year, DateTimeKind.Local, year, month);
		}
		
		/// <summary>
		/// Sets the full year (4 digits for 4-digit years) of this date, according to local time.
		/// </summary>
		/// <param name="year"> The 4 digit year. </param>
		/// <param name="month"> The month (0-11). </param>
		/// <param name="day"> The day of the month (1-31). </param>
		/// <returns> The number of milliseconds elapsed since January 1, 1970, 00:00:00 UTC for
		/// the new date. </returns>
		public double SetFullYear(double year, double month, double day)
		{
			return SetDateComponents(DateComponent.Year, DateTimeKind.Local, year, month, day);
		}
		
		/// <summary>
		/// Sets the month of this date, according to local time.
		/// </summary>
		/// <param name="month"> The month (0-11). </param>
		/// <returns> The number of milliseconds elapsed since January 1, 1970, 00:00:00 UTC for
		/// the new date. </returns>
		public double SetMonth(double month)
		{
			return SetDateComponents(DateComponent.Month, DateTimeKind.Local, month);
		}

		/// <summary>
		/// Sets the month of this date, according to local time.
		/// </summary>
		/// <param name="month"> The month (0-11). </param>
		/// <param name="day"> The day of the month (1-31). </param>
		/// <returns> The number of milliseconds elapsed since January 1, 1970, 00:00:00 UTC for
		/// the new date. </returns>
		public double SetMonth(double month, double day)
		{
			return SetDateComponents(DateComponent.Month, DateTimeKind.Local, month, day);
		}

		/// <summary>
		/// Sets the day of this date, according to local time.
		/// </summary>
		/// <param name="day"> The day of the month (1-31). </param>
		/// <returns> The number of milliseconds elapsed since January 1, 1970, 00:00:00 UTC for
		/// the new date. </returns>
		public double SetDate(double day)
		{
			return SetDateComponents(DateComponent.Day, DateTimeKind.Local, day);
		}

		/// <summary>
		/// Sets the hours component of this date, according to local time.
		/// </summary>
		/// <param name="hour"> The number of hours since midnight (0-23). </param>
		/// <returns> The number of milliseconds elapsed since January 1, 1970, 00:00:00 UTC for
		/// the new date. </returns>
		public double SetHours(double hour)
		{
			return SetDateComponents(DateComponent.Hour, DateTimeKind.Local, hour);
		}

		/// <summary>
		/// Sets the hours component of this date, according to local time.
		/// </summary>
		/// <param name="hour"> The number of hours since midnight (0-23). </param>
		/// <param name="minute"> The number of minutes since the hour (0-59). </param>
		/// <returns> The number of milliseconds elapsed since January 1, 1970, 00:00:00 UTC for
		/// the new date. </returns>
		public double SetHours(double hour, double minute)
		{
			return SetDateComponents(DateComponent.Hour, DateTimeKind.Local, hour, minute);
		}

		/// <summary>
		/// Sets the hours component of this date, according to local time.
		/// </summary>
		/// <param name="hour"> The number of hours since midnight (0-23). </param>
		/// <param name="minute"> The number of minutes since the hour (0-59). </param>
		/// <param name="second"> The number of seconds since the minute (0-59). </param>
		/// <returns> The number of milliseconds elapsed since January 1, 1970, 00:00:00 UTC for
		/// the new date. </returns>
		public double SetHours(double hour, double minute, double second)
		{
			return SetDateComponents(DateComponent.Hour, DateTimeKind.Local, hour, minute, second);
		}

		/// <summary>
		/// Sets the hours component of this date, according to local time.
		/// </summary>
		/// <param name="hour"> The number of hours since midnight (0-23). </param>
		/// <param name="minute"> The number of minutes since the hour (0-59). </param>
		/// <param name="second"> The number of seconds since the minute (0-59). </param>
		/// <param name="millisecond"> The number of milliseconds since the second (0-999). </param>
		/// <returns> The number of milliseconds elapsed since January 1, 1970, 00:00:00 UTC for
		/// the new date. </returns>
		public double SetHours(double hour, double minute, double second, double millisecond)
		{
			return SetDateComponents(DateComponent.Hour, DateTimeKind.Local, hour, minute, second, millisecond);
		}

		/// <summary>
		/// Sets the minutes component of this date, according to local time.
		/// </summary>
		/// <param name="minute"> The number of minutes since the hour (0-59). </param>
		/// <returns> The number of milliseconds elapsed since January 1, 1970, 00:00:00 UTC for
		/// the new date. </returns>
		public double SetMinutes(double minute)
		{
			return SetDateComponents(DateComponent.Minute, DateTimeKind.Local, minute);
		}

		/// <summary>
		/// Sets the minutes component of this date, according to local time.
		/// </summary>
		/// <param name="minute"> The number of minutes since the hour (0-59). </param>
		/// <param name="second"> The number of seconds since the minute (0-59). </param>
		/// <returns> The number of milliseconds elapsed since January 1, 1970, 00:00:00 UTC for
		/// the new date. </returns>
		public double SetMinutes(double minute, double second)
		{
			return SetDateComponents(DateComponent.Minute, DateTimeKind.Local, minute, second);
		}

		/// <summary>
		/// Sets the minutes component of this date, according to local time.
		/// </summary>
		/// <param name="minute"> The number of minutes since the hour (0-59). </param>
		/// <param name="second"> The number of seconds since the minute (0-59). </param>
		/// <param name="millisecond"> The number of milliseconds since the second (0-999). </param>
		/// <returns> The number of milliseconds elapsed since January 1, 1970, 00:00:00 UTC for
		/// the new date. </returns>
		public double SetMinutes(double minute, double second, double millisecond)
		{
			return SetDateComponents(DateComponent.Minute, DateTimeKind.Local, minute, second, millisecond);
		}

		/// <summary>
		/// Sets the seconds component of this date, according to local time.
		/// </summary>
		/// <param name="second"> The number of seconds since the minute (0-59). </param>
		/// <returns> The number of milliseconds elapsed since January 1, 1970, 00:00:00 UTC for
		/// the new date. </returns>
		public double SetSeconds(double second)
		{
			return SetDateComponents(DateComponent.Second, DateTimeKind.Local, second);
		}

		/// <summary>
		/// Sets the seconds component of this date, according to local time.
		/// </summary>
		/// <param name="second"> The number of seconds since the minute (0-59). </param>
		/// <param name="millisecond"> The number of milliseconds since the second (0-999). </param>
		/// <returns> The number of milliseconds elapsed since January 1, 1970, 00:00:00 UTC for
		/// the new date. </returns>
		public double SetSeconds(double second, double millisecond)
		{
			return SetDateComponents(DateComponent.Second, DateTimeKind.Local, second, millisecond);
		}

		/// <summary>
		/// Sets the milliseconds component of this date, according to local time.
		/// </summary>
		/// <param name="millisecond"> The number of milliseconds since the second (0-999). </param>
		/// <returns> The number of milliseconds elapsed since January 1, 1970, 00:00:00 UTC for
		/// the new date. </returns>
		public double SetMilliseconds(double millisecond)
		{
			return SetDateComponents(DateComponent.Millisecond, DateTimeKind.Local, millisecond);
		}

		/// <summary>
		/// Sets the full year (4 digits for 4-digit years) of this date, according to universal time.
		/// </summary>
		/// <param name="year"> The 4 digit year. </param>
		/// <returns> The number of milliseconds elapsed since January 1, 1970, 00:00:00 UTC for
		/// the new date. </returns>
		public double SetUTCFullYear(double year)
		{
			return SetDateComponents(DateComponent.Year, DateTimeKind.Utc, year);
		}

		/// <summary>
		/// Sets the full year (4 digits for 4-digit years) of this date, according to universal time.
		/// </summary>
		/// <param name="year"> The 4 digit year. </param>
		/// <param name="month"> The month (0-11). </param>
		/// <returns> The number of milliseconds elapsed since January 1, 1970, 00:00:00 UTC for
		/// the new date. </returns>
		public double SetUTCFullYear(double year, double month)
		{
			return SetDateComponents(DateComponent.Year, DateTimeKind.Utc, year, month);
		}

		/// <summary>
		/// Sets the full year (4 digits for 4-digit years) of this date, according to universal time.
		/// </summary>
		/// <param name="year"> The 4 digit year. </param>
		/// <param name="month"> The month (0-11). </param>
		/// <param name="day"> The day of the month (1-31). </param>
		/// <returns> The number of milliseconds elapsed since January 1, 1970, 00:00:00 UTC for
		/// the new date. </returns>
		public double SetUTCFullYear(double year, double month, double day)
		{
			return SetDateComponents(DateComponent.Year, DateTimeKind.Utc, year, month, day);
		}

		/// <summary>
		/// Sets the month of this date, according to universal time.
		/// </summary>
		/// <param name="month"> The month (0-11). </param>
		/// <returns> The number of milliseconds elapsed since January 1, 1970, 00:00:00 UTC for
		/// the new date. </returns>
		public double SetUTCMonth(double month)
		{
			return SetDateComponents(DateComponent.Month, DateTimeKind.Utc, month);
		}

		/// <summary>
		/// Sets the month of this date, according to universal time.
		/// </summary>
		/// <param name="month"> The month (0-11). </param>
		/// <param name="day"> The day of the month (1-31). </param>
		/// <returns> The number of milliseconds elapsed since January 1, 1970, 00:00:00 UTC for
		/// the new date. </returns>
		public double SetUTCMonth(double month, double day)
		{
			return SetDateComponents(DateComponent.Month, DateTimeKind.Utc, month, day);
		}

		/// <summary>
		/// Sets the day of this date, according to universal time.
		/// </summary>
		/// <param name="day"> The day of the month (1-31). </param>
		/// <returns> The number of milliseconds elapsed since January 1, 1970, 00:00:00 UTC for
		/// the new date. </returns>
		public double SetUTCDate(double day)
		{
			return SetDateComponents(DateComponent.Day, DateTimeKind.Utc, day);
		}

		/// <summary>
		/// Sets the hours component of this date, according to universal time.
		/// </summary>
		/// <param name="hour"> The number of hours since midnight (0-23). </param>
		/// <returns> The number of milliseconds elapsed since January 1, 1970, 00:00:00 UTC for
		/// the new date. </returns>
		public double SetUTCHours(double hour)
		{
			return SetDateComponents(DateComponent.Hour, DateTimeKind.Utc, hour);
		}

		/// <summary>
		/// Sets the hours component of this date, according to universal time.
		/// </summary>
		/// <param name="hour"> The number of hours since midnight (0-23). </param>
		/// <param name="minute"> The number of minutes since the hour (0-59). </param>
		/// <returns> The number of milliseconds elapsed since January 1, 1970, 00:00:00 UTC for
		/// the new date. </returns>
		public double SetUTCHours(double hour, double minute)
		{
			return SetDateComponents(DateComponent.Hour, DateTimeKind.Utc, hour, minute);
		}

		/// <summary>
		/// Sets the hours component of this date, according to universal time.
		/// </summary>
		/// <param name="hour"> The number of hours since midnight (0-23). </param>
		/// <param name="minute"> The number of minutes since the hour (0-59). </param>
		/// <param name="second"> The number of seconds since the minute (0-59). </param>
		/// <returns> The number of milliseconds elapsed since January 1, 1970, 00:00:00 UTC for
		/// the new date. </returns>
		public double SetUTCHours(double hour, double minute, double second)
		{
			return SetDateComponents(DateComponent.Hour, DateTimeKind.Utc, hour, minute, second);
		}

		/// <summary>
		/// Sets the hours component of this date, according to universal time.
		/// </summary>
		/// <param name="hour"> The number of hours since midnight (0-23). </param>
		/// <param name="minute"> The number of minutes since the hour (0-59). </param>
		/// <param name="second"> The number of seconds since the minute (0-59). </param>
		/// <param name="millisecond"> The number of milliseconds since the second (0-999). </param>
		/// <returns> The number of milliseconds elapsed since January 1, 1970, 00:00:00 UTC for
		/// the new date. </returns>
		public double SetUTCHours(double hour, double minute, double second, double millisecond)
		{
			return SetDateComponents(DateComponent.Hour, DateTimeKind.Utc, hour, minute, second, millisecond);
		}

		/// <summary>
		/// Sets the minutes component of this date, according to universal time.
		/// </summary>
		/// <param name="minute"> The number of minutes since the hour (0-59). </param>
		/// <returns> The number of milliseconds elapsed since January 1, 1970, 00:00:00 UTC for
		/// the new date. </returns>
		public double SetUTCMinutes(double minute)
		{
			return SetDateComponents(DateComponent.Minute, DateTimeKind.Utc, minute);
		}

		/// <summary>
		/// Sets the minutes component of this date, according to universal time.
		/// </summary>
		/// <param name="minute"> The number of minutes since the hour (0-59). </param>
		/// <param name="second"> The number of seconds since the minute (0-59). </param>
		/// <returns> The number of milliseconds elapsed since January 1, 1970, 00:00:00 UTC for
		/// the new date. </returns>
		public double SetUTCMinutes(double minute, double second)
		{
			return SetDateComponents(DateComponent.Minute, DateTimeKind.Utc, minute, second);
		}

		/// <summary>
		/// Sets the minutes component of this date, according to universal time.
		/// </summary>
		/// <param name="minute"> The number of minutes since the hour (0-59). </param>
		/// <param name="second"> The number of seconds since the minute (0-59). </param>
		/// <param name="millisecond"> The number of milliseconds since the second (0-999). </param>
		/// <returns> The number of milliseconds elapsed since January 1, 1970, 00:00:00 UTC for
		/// the new date. </returns>
		public double SetUTCMinutes(double minute, double second, double millisecond)
		{
			return SetDateComponents(DateComponent.Minute, DateTimeKind.Utc, minute, second, millisecond);
		}

		/// <summary>
		/// Sets the seconds component of this date, according to universal time.
		/// </summary>
		/// <param name="second"> The number of seconds since the minute (0-59). </param>
		/// <returns> The number of milliseconds elapsed since January 1, 1970, 00:00:00 UTC for
		/// the new date. </returns>
		public double SetUTCSeconds(double second)
		{
			return SetDateComponents(DateComponent.Second, DateTimeKind.Utc, second);
		}

		/// <summary>
		/// Sets the seconds component of this date, according to universal time.
		/// </summary>
		/// <param name="second"> The number of seconds since the minute (0-59). </param>
		/// <param name="millisecond"> The number of milliseconds since the second (0-999). </param>
		/// <returns> The number of milliseconds elapsed since January 1, 1970, 00:00:00 UTC for
		/// the new date. </returns>
		public double SetUTCSeconds(double second, double millisecond)
		{
			return SetDateComponents(DateComponent.Second, DateTimeKind.Utc, second, millisecond);
		}

		/// <summary>
		/// Sets the milliseconds component of this date, according to universal time.
		/// </summary>
		/// <param name="millisecond"> The number of milliseconds since the second (0-999). </param>
		/// <returns> The number of milliseconds elapsed since January 1, 1970, 00:00:00 UTC for
		/// the new date. </returns>
		public double SetUTCMilliseconds(double millisecond)
		{
			return SetDateComponents(DateComponent.Millisecond, DateTimeKind.Utc, millisecond);
		}

		/// <summary>
		/// Sets the date and time value of ths date.
		/// </summary>
		/// <param name="millisecond"> The number of milliseconds since January 1, 1970, 00:00:00 UTC. </param>
		/// <returns> The number of milliseconds elapsed since January 1, 1970, 00:00:00 UTC for
		/// the new date. </returns>
		public double SetTime(double millisecond)
		{
			this.value = ToDateTime(millisecond);
			return this.ValueInMilliseconds;
		}

		/// <summary>
		/// Returns the date as a string.
		/// </summary>
		/// <returns> The date as a string. </returns>
		public string ToDateString()
		{
			if (this.value == InvalidDate)
				return "Invalid Date";
			return this.value.ToLocalTime().ToString("ddd MMM dd yyyy", System.Globalization.DateTimeFormatInfo.InvariantInfo);
		}

		/// <summary>
		/// Returns the date as a string using GMT (Greenwich Mean Time).
		/// </summary>
		/// <returns> The date as a string. </returns>
		public string ToGMTString()
		{
			if (this.value == InvalidDate)
				return "Invalid Date";
			return this.value.ToUniversalTime().ToString("ddd, dd MMM yyyy HH:mm:ss 'GMT'", System.Globalization.DateTimeFormatInfo.InvariantInfo);
		}

		/// <summary>
		/// Returns the date as a string using GMT (Greenwich Mean Time).
		/// </summary>
		/// <returns> The date as a string. </returns>
		public string ToISOString(ScriptEngine engine)
		{
			if (this.value == InvalidDate)
				throw new JavaScriptException(engine, "RangeError", "The date is invalid");
			return this.value.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", System.Globalization.DateTimeFormatInfo.InvariantInfo);
		}

		/// <summary>
		/// Used by the JSON.stringify to transform objects prior to serialization.
		/// </summary>
		/// <param name="thisObj"> The object that is being operated on. </param>
		/// <param name="key"> Unused. </param>
		/// <returns> The date as a serializable string. </returns>
		public static object ToJSON(ObjectInstance thisObj, string key)
		{
			var number = TypeConverter.ToPrimitive(thisObj, PrimitiveTypeHint.Number);
			if (number is double && (double.IsInfinity((double)number) || double.IsNaN((double)number)))
				return Null.Value;
			return thisObj.CallMemberFunction("toISOString");
		}

		/// <summary>
		/// Returns the date as a string using the current locale settings.
		/// </summary>
		/// <returns></returns>
		public string ToLocaleDateString()
		{
			if (this.value == InvalidDate)
				return "Invalid Date";
			return this.value.ToLocalTime().ToString("D", System.Globalization.DateTimeFormatInfo.CurrentInfo);
		}

		/// <summary>
		/// Returns the date and time as a string using the current locale settings.
		/// </summary>
		/// <returns></returns>
		public string ToLocaleString()
		{
			if (this.value == InvalidDate)
				return "Invalid Date";
			return this.value.ToLocalTime().ToString("F", System.Globalization.DateTimeFormatInfo.CurrentInfo);
		}

		/// <summary>
		/// Returns the time as a string using the current locale settings.
		/// </summary>
		/// <returns></returns>
		public string ToLocaleTimeString()
		{
			if (this.value == InvalidDate)
				return "Invalid Date";
			return this.value.ToLocalTime().ToString("T", System.Globalization.DateTimeFormatInfo.CurrentInfo);
		}

		/// <summary>
		/// Returns a string representing the date and time.
		/// </summary>
		/// <returns> A string representing the date and time. </returns>
		public static string ToString(object thisRef)
		{
			// As of ES6, this method is generic.
			if ((thisRef is Date) == false)
				return "Invalid Date";

			var instance = (Date)thisRef;
			if (instance.value == InvalidDate)
				return "Invalid Date";

			var dateTime = instance.value.ToLocalTime();
			return dateTime.ToString("ddd MMM dd yyyy HH:mm:ss ", System.Globalization.DateTimeFormatInfo.InvariantInfo) +
				ToTimeZoneString(dateTime);
		}

		/// <summary>
		/// Returns the time as a string.
		/// </summary>
		/// <returns></returns>
		public string ToTimeString()
		{
			if (this.value == InvalidDate)
				return "Invalid Date";

			var dateTime = this.value.ToLocalTime();
			return dateTime.ToString("HH:mm:ss ", System.Globalization.DateTimeFormatInfo.InvariantInfo) +
				ToTimeZoneString(dateTime);
		}

		/// <summary>
		/// Returns the date as a string using UTC (universal time).
		/// </summary>
		/// <returns></returns>
		public string ToUTCString()
		{
			if (this.value == InvalidDate)
				return "Invalid Date";
			return this.value.ToUniversalTime().ToString("ddd, dd MMM yyyy HH:mm:ss 'GMT'", System.Globalization.DateTimeFormatInfo.InvariantInfo);
		}

		/// <summary>
		/// Returns the primitive value of this object.
		/// </summary>
		/// <returns> The primitive value of this object. </returns>
		public double ValueOf()
		{
			return this.ValueInMilliseconds;
		}



		//	 STATIC JAVASCRIPT METHODS (FROM DATECONSTRUCTOR)
		//_________________________________________________________________________________________

		/// <summary>
		/// Returns the current date and time as the number of milliseconds elapsed since January 1,
		/// 1970, 00:00:00 UTC.
		/// </summary>
		/// <returns> The current date and time as the number of milliseconds elapsed since January 1,
		/// 1970, 00:00:00 UTC. </returns>
		public static double Now()
		{
			return ToJSDate(GetNow());
		}

		/// <summary>
		/// Given the components of a UTC date, returns the number of milliseconds since January 1,
		/// 1970, 00:00:00 UTC to that date.
		/// </summary>
		/// <param name="year"> The full year. </param>
		/// <param name="month"> The month as an integer between 0 and 11 (january to december). </param>
		/// <param name="day"> The day of the month, from 1 to 31.  Defaults to 1. </param>
		/// <param name="hour"> The number of hours since midnight, from 0 to 23.  Defaults to 0. </param>
		/// <param name="minute"> The number of minutes, from 0 to 59.  Defaults to 0. </param>
		/// <param name="second"> The number of seconds, from 0 to 59.  Defaults to 0. </param>
		/// <param name="millisecond"> The number of milliseconds, from 0 to 999.  Defaults to 0. </param>
		/// <returns> The number of milliseconds since January 1, 1970, 00:00:00 UTC to the given
		/// date. </returns>
		/// <remarks>
		/// This method differs from the Date constructor in two ways:
		/// 1. The date components are specified in UTC time rather than local time.
		/// 2. A number is returned instead of a Date instance.
		/// 
		/// If any of the parameters are out of range, then the other values are modified accordingly.
		/// </remarks>
		public static double UTC(int year, int month, int day, int hour,
			int minute, int second, int millisecond)
		{
			return ToJSDate(ToDateTime(year, month, day, hour, minute, second, millisecond, DateTimeKind.Utc));
		}

		/// <summary>
		/// Parses a string representation of a date, and returns the number of milliseconds since
		/// January 1, 1970, 00:00:00 UTC.
		/// </summary>
		/// <param name="dateStr"> A string representing a date, expressed in RFC 1123 format. </param>
		public static double Parse(string dateStr)
		{
			return ToJSDate(DateParser.Parse(dateStr));
		}



		//	 PRIVATE IMPLEMENTATION METHODS
		//_________________________________________________________________________________________


		private enum DateComponent
		{
			Year = 0,
			Month = 1,
			Day = 2,
			Hour = 3,
			Minute = 4,
			Second = 5,
			Millisecond = 6,
			DayOfWeek,
		}

		/// <summary>
		/// Gets a single component of this date.
		/// </summary>
		/// <param name="component"> The date component to extract. </param>
		/// <param name="localOrUniversal"> Indicates whether to retrieve the component in local
		/// or universal time. </param>
		/// <returns> The date component value, or <c>NaN</c> if the date is invalid. </returns>
		private double GetDateComponent(DateComponent component, DateTimeKind localOrUniversal)
		{
			if (this.value == InvalidDate)
				return double.NaN;

			// Convert the date to local or universal time.
			switch (localOrUniversal)
			{
				case DateTimeKind.Local:
					this.value = this.Value.ToLocalTime();
					break;
				case DateTimeKind.Utc:
					this.value = this.Value.ToUniversalTime();
					break;
				default:
					throw new ArgumentOutOfRangeException("localOrUniversal");
			}

			// Extract the requested component.
			switch (component)
			{
				case DateComponent.Year:
					return this.value.Year;
				case DateComponent.Month:
					return this.value.Month - 1;	// Javascript month is 0-11.
				case DateComponent.Day:
					return this.value.Day;
				case DateComponent.DayOfWeek:
					return (double)this.value.DayOfWeek;
				case DateComponent.Hour:
					return this.value.Hour;
				case DateComponent.Minute:
					return this.value.Minute;
				case DateComponent.Second:
					return this.value.Second;
				case DateComponent.Millisecond:
					return this.value.Millisecond;
				default:
					throw new ArgumentOutOfRangeException("component");
			}
		}

		/// <summary>
		/// Sets one or more components of this date.
		/// </summary>
		/// <param name="firstComponent"> The first date component to set. </param>
		/// <param name="localOrUniversal"> Indicates whether to set the component(s) in local
		/// or universal time. </param>
		/// <param name="componentValues"> One or more date component values. </param>
		/// <returns> The number of milliseconds elapsed since January 1, 1970, 00:00:00 UTC for
		/// the new date. </returns>
		private double SetDateComponents(DateComponent firstComponent, DateTimeKind localOrUniversal, params double[] componentValues)
		{
			// Convert the date to local or universal time.
			switch (localOrUniversal)
			{
				case DateTimeKind.Local:
					this.value = this.Value.ToLocalTime();
					break;
				case DateTimeKind.Utc:
					this.value = this.Value.ToUniversalTime();
					break;
				default:
					throw new ArgumentOutOfRangeException("localOrUniversal");
			}

			// Get the current component values of the date.
			int[] allComponentValues = new int[7];
			allComponentValues[0] = this.value.Year;
			allComponentValues[1] = this.value.Month - 1;   // Javascript month is 0-11.
			allComponentValues[2] = this.value.Day;
			allComponentValues[3] = this.value.Hour;
			allComponentValues[4] = this.value.Minute;
			allComponentValues[5] = this.value.Second;
			allComponentValues[6] = this.value.Millisecond;

			// Overwrite the component values with the new ones that were passed in.
			for (int i = 0; i < componentValues.Length; i++)
			{
				double componentValue = componentValues[i];
				if (double.IsNaN(componentValue) || double.IsInfinity(componentValue))
				{
					this.value = InvalidDate;
					return this.ValueInMilliseconds;
				}
				allComponentValues[(int)firstComponent + i] = (int)componentValue;
			}

			// Construct a new date.
			this.value = ToDateTime(allComponentValues[0], allComponentValues[1], allComponentValues[2],
				allComponentValues[3], allComponentValues[4], allComponentValues[5], allComponentValues[6],
				localOrUniversal);

			// Return the date value.
			return this.ValueInMilliseconds;
		}

		/// <summary>
		/// Converts a .NET date into a javascript date.
		/// </summary>
		/// <param name="dateTime"> The .NET date. </param>
		/// <returns> The number of milliseconds since January 1, 1970, 00:00:00 UTC </returns>
		private static double ToJSDate(DateTime dateTime)
		{
			if (dateTime == InvalidDate)
				return double.NaN;
			return dateTime.ToUniversalTime().Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;
		}

		/// <summary>
		/// Converts a javascript date into a .NET date.
		/// </summary>
		/// <param name="milliseconds"> The number of milliseconds since January 1, 1970, 00:00:00 UTC. </param>
		/// <returns> The equivalent .NET date. </returns>
		private static DateTime ToDateTime(double milliseconds)
		{
			// Check if the milliseconds value is out of range.
			if (double.IsNaN(milliseconds) || milliseconds < -31557600000000 || milliseconds > 31557600000000)
				return InvalidDate;

			return new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)
				.AddMilliseconds(milliseconds);
		}

		/// <summary>
		/// Given the components of a date, returns the equivalent .NET date.
		/// </summary>
		/// <param name="year"> The full year. </param>
		/// <param name="month"> The month as an integer between 0 and 11 (january to december). </param>
		/// <param name="day"> The day of the month, from 1 to 31.  Defaults to 1. </param>
		/// <param name="hour"> The number of hours since midnight, from 0 to 23.  Defaults to 0. </param>
		/// <param name="minute"> The number of minutes, from 0 to 59.  Defaults to 0. </param>
		/// <param name="second"> The number of seconds, from 0 to 59.  Defaults to 0. </param>
		/// <param name="millisecond"> The number of milliseconds, from 0 to 999.  Defaults to 0. </param>
		/// <param name="kind"> Indicates whether the components are in UTC or local time. </param>
		/// <returns> The equivalent .NET date. </returns>
		private static DateTime ToDateTime(int year, int month, int day, int hour, int minute, int second, int millisecond, DateTimeKind kind)
		{
			if (month >= 0 && month < 12 &&
				day >= 1 && day <= DateTime.DaysInMonth(year, month + 1) &&
				hour >= 0 && hour < 24 &&
				minute >= 0 && minute < 60 &&
				second >= 0 && second < 60 &&
				millisecond >= 0 && millisecond < 1000)
			{
				// All parameters are in range.
				return new DateTime(year, month + 1, day, hour, minute, second, millisecond, kind);
			}
			else
			{
				// One or more parameters are out of range.
				try
				{
					DateTime value = new DateTime(year, 1, 1, 0, 0, 0, kind);
					value = value.AddMonths(month);
					if (day != 1)
						value = value.AddDays(day - 1);
					if (hour != 0)
						value = value.AddHours(hour);
					if (minute != 0)
						value = value.AddMinutes(minute);
					if (second != 0)
						value = value.AddSeconds(second);
					if (millisecond != 0)
						value = value.AddMilliseconds(millisecond);
					return value;
				}
				catch (ArgumentOutOfRangeException)
				{
					// One or more of the parameters was NaN or way too big or way too small.
					// Return a sentinel invalid date.
					return InvalidDate;
				}
			}
		}

		/// <summary>
		/// Gets the current time and date.
		/// </summary>
		/// <returns> The current time and date. </returns>
		private static DateTime GetNow()
		{
			return DateTime.Now;
		}

		/// <summary>
		/// Returns a string of the form "GMT+1200 (New Zealand Standard Time)".
		/// </summary>
		/// <param name="dateTime"> The date to get the time zone information from. </param>
		/// <returns> A string of the form "GMT+1200 (New Zealand Standard Time)". </returns>
		private static string ToTimeZoneString(DateTime dateTime)
		{
			var timeZone = TimeZoneInfo.Local;

			// Compute the time zone offset in hours-minutes.
			int offsetInMinutes = (int)timeZone.GetUtcOffset(dateTime).TotalMinutes;
			int hhmm = offsetInMinutes / 60 * 100 + offsetInMinutes % 60;

			// Get the time zone name.
			string zoneName;
			if (timeZone.IsDaylightSavingTime(dateTime))
				zoneName = timeZone.DaylightName;
			else
				zoneName = timeZone.StandardName;

			if (hhmm < 0)
				return string.Format("GMT{0:d4} ({1})", hhmm, zoneName);
			else
				return string.Format("GMT+{0:d4} ({1})", hhmm, zoneName);
		}
		
		/// <summary>
		/// Called when the Date object is invoked like a function, e.g. var x = Date().
		/// Returns a string representing the current time.
		/// </summary>
		public static string OnCall()
		{
			return Date.ToString(new Date());
		}
		
	}
}
