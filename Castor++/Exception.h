/*
 * $Id: ConfigException.h 64 2008-01-14 20:31:24Z phbaer $
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

#ifndef CASTOR_EXCEPTION_H
#define CASTOR_EXCEPTION_H 1

#ifndef _GNU_SOURCE
#  define _GNU_SOURCE
#endif
#include <stdio.h>

#include <stdlib.h>
#include <exception>
#include <string>
#include <iostream>
#include <cstdarg>

namespace castor {

	class Exception : public std::exception {

		protected:

			std::string reason;

		public:

			Exception(const std::string what = "unknown exception occured", ...) throw() :
				std::exception(), reason()
			{
				va_list params;
				va_start(params, what);
				setReason(what, params);
				va_end(params);
			}

			virtual ~Exception() throw() {
			}
			
			virtual const char *what() const throw() {
				return this->reason.c_str();
			}
			
		protected:

			void setReason(const std::string &what, va_list &args) {

				char *result = NULL;

				if (vasprintf(&result, what.c_str(), args) == -1) {
					std::cout << "Exception: Error while setting reason!" << std::endl;
				}

				this->reason = std::string(result);

				free(result);
			}
	};
}

std::ostream &operator << (std::ostream &os, const castor::Exception &x);

#endif /* CASTOR_EXCEPTION_H */

