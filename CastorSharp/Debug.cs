/*
 * $Id: Debug.cs 1840 2007-02-08 07:52:29Z phbaer $
 *
 *
 * Copyright 2005,2006 Carpe Noctem, Distributed Systems Group,
 * University of Kassel. All right reserved.
 *
 * The code is derived from the software contributed to Carpe Noctem by
 * the Carpe Noctem Team.
 *
 * The code is licensed under the Carpe Noctem Userfriendly BSD-Based
 * License (CNUBBL). Redistribution and use in source and binary forms,
 * with or without modification, are permitted provided that the
 * conditions of the CNUBBL are met.
 *
 * You should have received a copy of the CNUBBL along with this
 * software. The license is also available on our website:
 * http://carpenoctem.das-lab.net/license.txt
 *
 *
 * <description>
 */
using System;
using System.IO;

namespace Castor {

	/// The MessageType is nessesary for the debug-messages.
	public enum MsgLevel {
		Off     = 0,
		Error   = 1,
		Warning = 2,
		Info    = 3,
		Verbose = 4
	}

	/// <summary>This class is for filtering the debugmessages.</summary>
	/// <example>
	/// <code>
	///   // These should be set to default
	///   MsgSwitch trace = new MsgSwitch(MsgLevel.Off);
	///   MsgSwitch debug = new MsgSwitch(MsgLevel.Verbose);
	///
	///   if (String.Compare(args[0], "--verbose") == 0)
	///         trace.Level = MsgLevel.Verbose;
	///   
	///   if (trace.Info) {
	///     Trace.WriteLine("This is a info message");
	///   }
	///   
	///   Trace.WriteLineIf(trace.Verbose, String.Format(
	///       "The ball has a velocity of x:{0}, y:{0}, vx, vy));
	///   
	///   Debug.WriteLineIf(debug.Error, "This is a error message");
	/// </code>
	/// </example>
	public class MsgSwitch {
		protected MsgLevel level;
		
#region constructor
		/// Constructor for MsgSwitch
		/// <param name="level">The messagelevel for this switch</param>
		public MsgSwitch(MsgLevel level) {
			if ((level < MsgLevel.Off) || (level > MsgLevel.Verbose)) {
				throw new Exception("MessageLevel has to be in Range: 0 - 4");
			}
			else {
				this.level = level;
			}
		}
#endregion

		/// <summary> Sets or gets the level, at which level or below
		/// the message gets reported. <seealso cref="MsgLevel" /></summary>
		public MsgLevel Level {
			get { return this.level; }
			set { this.level = value; }
		}
		
		/// This is only for disabling the message. Returns always
		/// false.
		public bool Off {
			get { return false; }
		}
		
		/// Checks if the messagelevel is <c>Error</c> or lower.
		public bool Error {
			get { return (level >= MsgLevel.Error); }
		}

		/// Checks if the messagelevel is <c>Warning</c> or lower.
		public bool Warning {
			get { return (level >= MsgLevel.Warning); }
		}

		/// Checks if the messagelevel is <c>Info</c> or lower.
		public bool Info {
			get { return (level >= MsgLevel.Info); }
		}
		
		/// Checks if the messagelevel is <c>Verbose</c> or lower.
		public bool Verbose {
			get { return (level >= MsgLevel.Verbose); }
		}

	}

	public class Debug {
		protected static TextWriter tw = Console.Error;
		protected static bool flush = true;
		public static string Name = null;
		protected static bool lineActive = false;

		protected static void DateName() {
			if (!lineActive) {
				if (Name == null) {
					tw.Write("{0}: ", DateTime.Now.ToString("u"));
				} else {
					tw.Write("{0} {1}: ", DateTime.Now.ToString("u"), Name);
				}
			}
		}
		
		protected static void Flush() {
			if (flush) {
				tw.Flush();
			}
		}

		public static void WriteLine() {
			DateName();
			tw.WriteLine();
			Flush();
			lineActive = false;
		}
		
		public static void WriteLine(string msg) {
			DateName();
			tw.WriteLine(msg);
			Flush();
			lineActive = false;
		}

		public static void WriteLine(string msg, params object[] p) {
			DateName();
			tw.WriteLine(msg, p);
			Flush();
			lineActive = false;
		}

		public static void WriteLineIf(bool enabled, string msg) {
			if (enabled) {
				DateName();
				tw.WriteLine(msg);
				Flush();
				lineActive = false;
			}
		}

		public static void WriteLineIf(bool enabled, string msg, params object[] p) {
			if (enabled) {
				DateName();
				tw.WriteLine(msg, p);
				Flush();
				lineActive = false;
			}
		}

		public static void Write(string msg) {
			DateName();
			tw.Write(msg);
			Flush();
			lineActive = true;
		}

		public static void Write(string msg, params object[] p) {
			DateName();
			tw.Write(msg, p);
			Flush();
			lineActive = true;
		}

		public static void WriteIf(bool enabled, string msg) {
			if (enabled) {
				DateName();
				tw.WriteLine(msg);
				Flush();
				lineActive = true;
			}
		}

		public static void WriteIf(bool enabled, string msg, params object[] p) {
			if (enabled) {
				DateName();
				tw.WriteLine(msg, p);
				Flush();
				lineActive = true;
			}
		}

		public static void SetOutput(TextWriter output) {
			tw = output;
		}

		public static bool AutoFlush {
			set { flush = value; }
			get { return flush; }
		}
	}

	public class Trace {
		protected static TextWriter tw = Console.Out;
		protected static bool flush = true;
		public static string Name = null;
		protected static bool lineActive = false;

		protected static void DateName() {
			if (!lineActive) {
				if (Name == null) {
					tw.Write("{0}: ", DateTime.Now.ToString("u"));
				} else {
					tw.Write("{0} {1}: ", DateTime.Now.ToString("u"), Name);
				}
			}
		}
		
		protected static void Flush() {
			if (flush) {
				tw.Flush();
			}
		}
		
		public static void WriteLine() {
			DateName();
			tw.WriteLine();
			Flush();
			lineActive = false;
		}

		public static void WriteLine(string msg) {
			DateName();
			tw.WriteLine(msg);
			Flush();
			lineActive = false;
		}

		public static void WriteLine(string msg, params object[] p) {
			DateName();
			tw.WriteLine(msg, p);
			Flush();
			lineActive = false;
		}

		public static void WriteLineIf(bool enabled, string msg) {
			if (enabled) {
				DateName();
				tw.WriteLine(msg);
				Flush();
				lineActive = false;
			}
		}

		public static void WriteLineIf(bool enabled, string msg, params object[] p) {
			if (enabled) {
				DateName();
				tw.WriteLine(msg, p);
				Flush();
				lineActive = false;
			}
		}

		public static void Write(string msg) {
			DateName();
			tw.Write(msg);
			Flush();
			lineActive = true;
		}

		public static void Write(string msg, params object[] p) {
			DateName();
			tw.Write(msg, p);
			Flush();
			lineActive = true;
		}

		public static void WriteIf(bool enabled, string msg) {
			if (enabled) {
				DateName();
				tw.WriteLine(msg);
				Flush();
				lineActive = true;
			}
		}

		public static void WriteIf(bool enabled, string msg, params object[] p) {
			if (enabled) {
				DateName();
				tw.WriteLine(msg, p);
				Flush();
				lineActive = true;
			}
		}

		public static void SetOutput(TextWriter output) {
			tw = output;
		}

		public static bool AutoFlush {
			set { flush = value; }
			get { return flush; }
		}
	}
}
