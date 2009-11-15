/*
 * $Id: SystemConfig.cs 2427 2007-07-04 13:42:38Z phbaer $
 *
 *
 * Copyright 2005-2007 Carpe Noctem, Distributed Systems Group,
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
 */

using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Collections.Generic;

namespace Castor {
	
	public class SystemConfig {
		protected static bool initialized = false;
		protected static string rootPath = null;
		protected static string libPath = null;
		protected static string logPath = null;
		protected static string configPath = null;
		protected static string hostname = null;
		protected static Dictionary<string, Configuration> configs = null;
		protected static Object lockObject = new Object();

		public Configuration this[string s] {
			get {
				Configuration result = null;

				lock(configs) {
					if (initialized) {
						if (configs.ContainsKey(s)) {
							result = configs[s];
						
						} else {
							FileInfo fi = null;
							string filename = null;
							
							if ((s != null) && (s.Length > 0)) {
								filename = String.Format("{0}{1}/{2}.conf", configPath, hostname, s); 
								fi = new FileInfo(filename);

								if (fi.Exists) {
									result = new Configuration(filename);
									configs.Add(s, result);
									
								} else {
									filename = String.Format("{0}{1}.conf", configPath, s);
									fi = new FileInfo(filename);

									if (fi.Exists) {
										result = new Configuration(filename);
										configs.Add(s, result);
									} else {
										filename = String.Format("{0}.conf", s);
										fi = new FileInfo(filename);

										Console.WriteLine(filename);
										if (fi.Exists) {
											result = new Configuration(filename);
											configs.Add(s, result);
										}
									}
								}
							}
						}
					}
				}

				return result;
			}
		}
		
		public string RootPath {
			get { return rootPath; }
		}

		public string LibPath {
			get { return libPath; }
		}

		public string LogPath {
			get { return logPath; }
		}

		public string ConfigPath {
			get { return configPath; }
		}

		/// <summary>Adds the CN root directory to the given path if it is not absolute. <c>path</c> is sanitized, i.e. invalid characters may be trimmed.</summary>
		/// <param name="path">A path, either absolute or not</param>
		/// <returns>If <c>path</c> is not absolute, a new path is returned which contains the CN root path combined with <c>path</c>. If <c>path</c> is absolute, nothing is combined and <c>path</c> is returned</returns>
		public string CompletePath(string path) {
			// Sanitize path
			if ((path != null) && (path.Length > 0)) {
				// Add a trainling path separator
				path = path.TrimEnd(new char[] {
						Path.DirectorySeparatorChar,
						Path.AltDirectorySeparatorChar,
				});
				path += Path.DirectorySeparatorChar;

				return Path.Combine(rootPath, path);
			}

			return path;
		}

		// Static constructor
		static SystemConfig() {
		}

		// Default constructor
		public SystemConfig() : this("ES_ROOT", "ES_CONFIG_ROOT") { }

		// Default constructor
		public SystemConfig(string envRoot, string envConfigRoot) {
			char[] trimSlash = new char[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };

			lock(lockObject) {

				if (!initialized) {

					rootPath = Environment.GetEnvironmentVariable(envRoot);
					configPath = Environment.GetEnvironmentVariable(envConfigRoot);
						
					if (rootPath != null) {

						rootPath = rootPath.TrimEnd(trimSlash) + Path.DirectorySeparatorChar;

						if (!Directory.Exists(rootPath)) {
							rootPath = null;
						}

					}

					if (rootPath == null) {

						rootPath = AppDomain.CurrentDomain.BaseDirectory.TrimEnd(trimSlash);

						int lastSlash = rootPath.LastIndexOfAny(trimSlash);

						if (lastSlash > 0) {
							rootPath = rootPath.Substring(0, lastSlash).TrimEnd(trimSlash);
						}

						rootPath += Path.DirectorySeparatorChar;

						if (!Directory.Exists(rootPath)) {
							rootPath = "";
						}

					}

					if (configPath == null) {
						configPath = rootPath + "etc" + Path.DirectorySeparatorChar;

						if (!Directory.Exists(configPath)) {
							configPath = String.Format("{0}{1}{0}", Path.DirectorySeparatorChar, "etc");
						}
					}

					libPath = Path.Combine(rootPath, "lib/");
					logPath = Path.Combine(rootPath, "log/");

//					Debug.WriteLine("Root: {0}", rootPath);
//					Debug.WriteLine("Conf: {0}", configPath);
//					Debug.WriteLine("lib:  {0}", libPath);

					hostname = Dns.GetHostName();
					configs = new Dictionary<string, Configuration>();

					initialized = true;
				}
			}
		}
	}
}

