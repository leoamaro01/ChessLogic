using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PixelDashCore.ChessLogic
{
    [System.Serializable]
    public class InvalidMoveException : System.Exception
    {
        public InvalidMoveException() { }
        public InvalidMoveException(string message) : base(message) { }
        public InvalidMoveException(string message, System.Exception inner) : base(message, inner) { }
        protected InvalidMoveException(
            System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}