using PollStar.Core.ErrorCodes;

namespace PollStar.Polls.ErrorCodes;

public abstract class PollStarPollErrorCode : PollStarErrorCode
{
    public static readonly PollStarPollErrorCode PollNotFound = new PollNotFoundErrorCode();
    public static readonly PollStarPollErrorCode PollPersistenceFailed = new PollPersistenceFailedErrorCode();
    public static readonly PollStarPollErrorCode PollNameInvalid = new PollNameInvalidErrorCode();
    public static readonly PollStarPollErrorCode PollDescriptionInvalid = new PollDescriptionInvalidErrorCode();

    public override string TranslationKey => $"Errors.Polls.{Code}";
}