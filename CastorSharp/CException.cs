using System;
using System.Runtime.Serialization;

namespace Castor {
	public class CException : System.Exception {
		public CException() : base() {
		}

		public CException(string message, params object[] args) :
			base(String.Format(message, args))
		{
		}

		public CException(SerializationInfo si, StreamingContext sc) :
			base(si, sc)
		{
		}

		public CException(Exception e, string message, params object[] args) :
			base(String.Format(message, args), e)
		{
		}

/*        public override string ToString()
        {
            return "** " + Message;
        }*/
	}
}
