/*
 * $Id: ConfigException.h 208 2008-02-12 23:43:03Z phbaer $
 *
 * Copyright 2008 Carpe Noctem, Distributed Systems Group,
 * University of Kassel. All right reserved.
 *
 * The code is licensed under the Carpe Noctem Userfriendly BSD-Based
 * License (CNUBBL). Redistribution and use in source and binary forms,
 * with or without modification, are permitted provided that the
 * conditions of the CNUBBL are met.
 *
 * You should have received a copy of the CNUBBL along with this
 * software. The license is also available on our website:
 * http://carpenoctem.das-lab.net/license.txt
 */

#ifndef CASTOR_CONFIGEXCEPTION_H
#define CASTOR_CONFIGEXCEPTION_H 1

#include "Exception.h"

namespace castor {

	class ConfigException : public Exception {

		public:

			ConfigException(const std::string what = "unknown config exception occured", ...) throw();

	};

}

#endif /* CASTOR_CONFIGEXCEPTION_H */

