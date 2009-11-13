/*
 * $Id: Arguments.cs 2390 2007-06-20 18:21:32Z phbaer $
 *
 *
 * Copyright 2005,2006,2007 Carpe Noctem, Distributed Systems Group,
 * University of Kassel. All right reserved.
 *
 * The code is derived from the software contributed to Carpe Noctem by
 * Philipp Baer <phbaer -at- npw.net>
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
 * Description:
 *
 * This class is a simple implementation of a commandline argument consumer.
 * It accept options (-option or --option) and parameters (arbitrary strings).
 * Options can either have an argument or not. Arguments are attached to
 * options using the ":" delimiter (no spaces are allowed). Each option can be
 * specified an arbitrary number of times. Some examples:
 *
 * parameters: filename.txt parameters: filename1.txt filename2.txt test
 * options:    --test --version -v --name:Max --name:Tom -h --name:"Ernst Egon"
 *
 * Parameters and options can be mixed.
 */

using System;
using System.IO;
using System.Collections.Generic;

namespace Castor {

	public class Arguments {
		// This list holds all the options that were received from the commandline.
		// Additionally, options with a default value are inserted here.
		protected Dictionary<string, List<string>> options = null;
		// This list holds all the options that are recognized by this class
		protected List<OptionInfo> optionsAvailable = null;
		// This list is used only for keeping track of already consumes options
		protected List<string> optionsFound = null;
		// This list holds all the parameters that were received from the commandline
		protected List<string> parameters = null;
		// Maximal number of parameters
		protected int maxParameters = -1;
		// Minimal number of parameters
		protected int minParameters = -1;

		protected class OptionInfo: IComparable {
			
			public string option = null;
			public string description = null;
			
			public OptionInfo(string opt, string dsc) {
				option = opt;
				description = dsc;
			}

			public int CompareTo(object o) {
				if (o is OptionInfo) {
					return this.option.CompareTo(((OptionInfo)o).option);
				}
				return -1;
			}
			public override bool Equals(object o) {
				return (CompareTo(o) == 0);
			}

			public override int GetHashCode() {
				return this.option.GetHashCode();
			}
		}

		/// Default constructor
		public Arguments() : this(-1, -1) {
		}

		/// Constructor that initialized the min and max parameter bounds
		public Arguments(int minParameters, int maxParameters) {
			this.options = new Dictionary<string, List<string>>();
			this.optionsAvailable = new List<OptionInfo>();
			this.optionsFound = new List<string>();
			this.parameters = new List<string>();
			this.minParameters = minParameters;
			this.maxParameters = maxParameters;
		}

		/// Set options that should be recognized by the argument parser
		/// <param name="optionSpec">
		/// Specifies the option type; consists of an option string with an
		/// optional argument which specifies whether or not this option
		/// accepts arguments:
		///
		/// [option string]+: one or more arguments may be passed
		/// [option string]*: zero or more arguments may be passed
		/// [option string]!: exactly one argument may be passed
		///
		/// Each option may be prefixed with a '?' if it is optional:
		///
		/// ?[option string]: argument is optional
		///
		/// If an argument is given, it is used as the default argument.
		///
		/// All flags can be mixed.
		/// </param>
		/// <param name="description">A textual description of the option</param>
		public void SetOption(string optionSpec, string description) {
			if (!this.optionsAvailable.Contains(new OptionInfo(optionSpec, description))) {
				this.optionsAvailable.Add(new OptionInfo(optionSpec, description));
			}
		}

		public void SetOption(string optionSpec) {
			SetOption(optionSpec, "No description available");
		}

		/// Returns all the values for an option
		public List<string> GetOptionValues(string option) {
			if (this.options.ContainsKey(option)) {
				return this.options[option];
			}

			return null;
		}

		/// Checks if a option was set
		public bool OptionIsSet(string option) {
			return this.options.ContainsKey(option);
		}

		// Gets or sets the maximal number of parameters accepted
		public int MaxParameters {
			get { return this.maxParameters; }
			set { this.maxParameters = value; }
		}

		// Gets or sets the minimal number of parameters accepted
		public int MinParameters {
			get { return this.minParameters; }
			set { this.minParameters = value; }
		}

		// Returns all the parameters
		public List<string> GetParameters() {
			return this.parameters;
		}

		// Splits a option specification
		protected string[] Split(string option, char[] trim) {
			string[] result = null;
			string tmp = option.Trim(trim);
			int colon = tmp.IndexOf(':');
			if (colon == -1) {
				colon = tmp.IndexOf('=');
			}

			if (colon != -1) {
				result = new string[] {
					tmp.Substring(0, colon),
					tmp.Substring(colon + 1, tmp.Length - (colon + 1))
				};
			} else {
				result = new string[] {
					tmp
				};
			}

			return result;
		}

		// Consumes command line arguments
		public void Consume(string[] args) {
			int i = 0;

			while (i < args.Length) {
				if ((args[i].Length > 1) &&
					((args[i][0] != '-') && (args[i][1] != '-')))
				{
					// This is a parameter, simply add it to the list of parameters
					if ((this.parameters.Count < this.maxParameters) || (this.maxParameters == -1)) {
						this.parameters.Add(args[i]);
					} else {
						throw new Exception("Too many parameters specified!");
					}

				} else {
					// This is an option
					bool found = false;
					string[] splitOption = Split(args[i], new char[] { '-' });

					// Compare the passed option to all the registered names
					foreach (OptionInfo io in this.optionsAvailable) {
						string o = io.option;

						// 0: no argument
						// 1: there may be an argument
						// 2: there must be an argument
						// 2: there must be exactly one argument
						int type = ((o[o.Length - 1] == '*') ? 1 :
									(o[o.Length - 1] == '+') ? 2 : 
									(o[o.Length - 1] == '!') ? 3 : 0);
						string option = Split(o, new char[] { '?', '*', '+', '!' })[0];

						if (splitOption[0] == option) {
							// Option was found, check constraints
							
							if (!this.options.ContainsKey(option)) {
								this.options.Add(option, new List<string>());
							}

							if ((type == 0) && (splitOption.Length == 2)) {
								throw new Exception(String.Format("This option does not allow an argument ({0})!", splitOption[0]));
							}

							if ((type == 2) && (splitOption.Length == 1)) {
								if ((i + 1) < args.Length) {
									this.options[option].Add(args[++i]);
								} else {
									throw new Exception(String.Format("This option requires an argument ({0})!", splitOption[0]));
								}
							}

							if ((type == 3) && (splitOption.Length == 1)) {
								if ((i + 1) < args.Length) {
									this.options[option].Add(args[++i]);
								} else {
									throw new Exception(String.Format("This option requires exactly one argument ({0})!", splitOption[0]));
								}
							}

							if (splitOption.Length == 2) {
								this.options[option].Add(splitOption[1]);
							}

							this.optionsFound.Add(o);

							found = true;
						}
					}

					if (!found) {
						throw new Exception(String.Format("Option not known ({0})!", args[i]));
					}
				}

				i++;
			}

			if ((this.maxParameters != -1) && (this.maxParameters < this.parameters.Count)) {
				throw new Exception("Too many parameters!");
			}

			if ((this.minParameters != -1) && (this.minParameters > this.parameters.Count)) {
				throw new Exception("Not enough parameters!");
			}

			// Process and check the options that were not found
			if (this.optionsAvailable.Count != this.optionsFound.Count) {
				foreach (OptionInfo oi in this.optionsAvailable) {
					string o = oi.option;

					if (!this.optionsFound.Contains(o)) {
						string[] splitOption = Split(o, new char[] { '?', '*', '+', '!' });

						if (splitOption.Length == 2) {
							List<string> list = new List<string>();
							list.Add(splitOption[1]);
							this.options.Add(splitOption[0], list);

						} else {
							if ((o.Length > 0) && (o[0] != '?')) {
								throw new Exception(String.Format("Missing required parameter ({0})!", splitOption[0]));
							}
						}
					}
				}
			}
		}

		protected string WrapString(string s, int length, int indent) {
			char[] delimiters = new char[] { ' ', '-', '.' };
			string result = "";
			int pos = -1;
			int oldpos = -1;
			int bol = 0;

			// Go through the string; tokenize at the given delimiters
			// Continue until no more delimiters have been found and the last
			// part of the string is shorter than the allowed length
			while ((((pos = s.IndexOfAny(delimiters, pos + 1)) != -1) || ((s.Length - bol) > length)) && (oldpos != s.Length - 1)) {
				// If no more delimiters were found, set the position to the string length
				if (pos == -1) {
					pos = s.Length - 1;
				}

				// Wrap a word
				//
				// If even the old position is too far from the begin of line (bol)
				// or the old position is currently at the bol (so the case below would
				// not lead to acceptable results) and the difference between old and
				// new position is too big, wrap a single word.
				if ((((oldpos - bol) > length) || (bol == oldpos + 1)) && ((pos - oldpos) > length)) {
					result += s.Substring(bol, length).TrimEnd();
					result += (result.EndsWith("-") ? "\n" : "-\n");
					result = result.PadRight(result.Length + indent);
					bol += length + 1;
					pos = bol - 1;

				// Otherwise, if a suitable delimiter was found, wrap at this position
				} else if ((pos - bol) > length) {
					result += s.Substring(bol, oldpos - bol + 1).TrimEnd() + '\n';
					result = result.PadRight(result.Length + indent);
					bol = oldpos + 1;
					pos = oldpos;
				}

				// Backup the position
				oldpos = pos;
			}
			// Add the tail
			result += (s.Substring(bol, s.Length - bol));

			return result;
		}

		public override string ToString() {
			string[] cmdline = Environment.GetCommandLineArgs();

			string aDsc = "";
			string a = "";

			int maxOptionLen = 0;

			// Get the maximal string length of all options
			foreach (OptionInfo oi in this.optionsAvailable) {
				string oTemp = oi.option.Trim(new char[] { '?', '*', '+', '!' });
				string[] args = oTemp.Split(new char[] { ':' });
				maxOptionLen = (args[0].Length > maxOptionLen ? args[0].Length : maxOptionLen);
			}

			foreach (OptionInfo oi in this.optionsAvailable) {
				bool opt = (oi.option[0] == '?');
				int type = ((oi.option[oi.option.Length - 1] == '*') ? 1 :
							(oi.option[oi.option.Length - 1] == '+') ? 2 :
							(oi.option[oi.option.Length - 1] == '!') ? 3 : 0);

				string oTemp = oi.option.Trim(new char[] { '?', '*', '+', '!' });
				string[] args = oTemp.Split(new char[] { ':' });

				a += " ";

				if (opt) { a += "["; }

				a += ("-" + args[0]);
				aDsc += String.Format("-{0}:{1}{2}",
						args[0],
						"".PadRight(maxOptionLen - args[0].Length + 1),
						WrapString(oi.description, 79 - maxOptionLen - 3, maxOptionLen + 3));

				if (type == 1) {
					a += "=<arg>*";
				}
				if (type == 2) {
					a += "=<arg>+";
				}
				if (type == 3) {
					a += "=<arg>";
				}

				if (opt) { a += "]"; }

				if (args.Length == 2) {
					aDsc += String.Format("\n{0}(Default: {1})",
							"".PadRight(maxOptionLen + 3),
							args[1]);
				}

				aDsc += "\n";
			}

			string p = "";
			if ((this.minParameters != -1) || (this.maxParameters > 0)) {
				p = " <parameter>";

				if (this.minParameters == this.maxParameters) {
					if (this.minParameters > 1) {
						p += String.Format("{{{0}}}", this.maxParameters);
					}
				} else {
					p += String.Format("{{{0}-{1}}}",
							(this.minParameters == -1 ? "" : this.minParameters.ToString()),
							(this.maxParameters == -1 ? "" : this.maxParameters.ToString()));
				}
			}

			return String.Format("{0}{1}{2}\n{3}", Path.GetFileName(cmdline[0]), a, p, aDsc);
		}
	}
}

