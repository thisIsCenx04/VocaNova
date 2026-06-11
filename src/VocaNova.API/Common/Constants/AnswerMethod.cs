namespace VocaNova.API.Common.Constants;

public static class AnswerMethod
{
    public const string MultipleChoice = "multiple_choice";
    public const string ExactTyping = "exact_typing";
    public const string AiTyping = "ai_typing";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        MultipleChoice,
        ExactTyping,
        AiTyping,
    };
}
