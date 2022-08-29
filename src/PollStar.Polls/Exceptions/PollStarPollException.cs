using PollStar.Core.Exceptions;
using PollStar.Polls.ErrorCodes;

namespace PollStar.Polls.Exceptions;

public class PollStarPollException : PollStarException
{
    internal PollStarPollException(PollStarPollErrorCode errorCode, string message, Exception? ex = null) : base(errorCode, message, ex)
    {
    }
}