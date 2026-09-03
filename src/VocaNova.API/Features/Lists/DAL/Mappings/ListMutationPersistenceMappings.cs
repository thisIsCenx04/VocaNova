using System.Linq.Expressions;
using VocaNova.API.Features.Lists.BLL.Models;
using VocaNova.API.Infrastructure.Persistence;
using VocaNova.API.Infrastructure.Persistence.Entities;

namespace VocaNova.API.Features.Lists.DAL.Mappings;

internal static class ListMutationPersistenceMappings
{
    public static readonly Expression<Func<UserList, ListOwnership>> ToListOwnership =
        list => new ListOwnership(list.ListId, list.UserId, list.Status, list.ListName);

    public static readonly Expression<Func<UserListWord, ListWordState>> ToListWordState =
        listWord => new ListWordState(
            listWord.UserId,
            listWord.ListId,
            listWord.WordId,
            listWord.Status);

    public static Expression<Func<UserListWord, ListWord>> ToListWord(
        VocaNovaDbContext dbContext,
        uint userId) =>
        listWord => new ListWord(
            listWord.WordId,
            listWord.Word.Word1,
            listWord.Word.WordSenses
                .OrderBy(sense => sense.SenseOrder)
                .ThenBy(sense => sense.SenseId)
                .Select(sense => sense.VietnameseMeaning)
                .FirstOrDefault(),
            dbContext.UserWordProgresses
                .Where(progress => progress.UserId == userId
                    && progress.WordId == listWord.WordId)
                .Select(progress => (int?)progress.CorrectCount)
                .FirstOrDefault() ?? 0,
            dbContext.UserWordProgresses
                .Where(progress => progress.UserId == userId
                    && progress.WordId == listWord.WordId)
                .Select(progress => (int?)progress.WrongCount)
                .FirstOrDefault() ?? 0,
            listWord.Note,
            listWord.AddedAt);

    public static UserListSummary ToUserListSummary(UserList list, int wordCount) =>
        new(list.ListId, list.ListName, wordCount, list.CreatedAt);

    public static PersonalTopic ToPersonalTopic(
        uint topicId,
        uint? listId,
        string name,
        string? nameVi,
        string? icon,
        int wordCount,
        bool containsWord) =>
        new(topicId, listId, name, nameVi, icon, wordCount, containsWord);
}
