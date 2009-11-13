/*
 * $Id: ConfigException.cpp 64 2008-01-14 20:31:24Z phbaer $
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

#include "ConfigException.h"

namespace castor {

	ConfigException::ConfigException(const std::string what, ...) throw() {
		va_list params;
		va_start(params, what);
		setReason(what, params);
		va_end(params);
	}

}

std::ostream &operator << (std::ostream &os, const castor::ConfigException &x) {
	return os << x.what();
}
