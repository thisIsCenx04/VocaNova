using System.Linq.Expressions;
using VocaNova.API.Features.Lists.BLL.Models;
using VocaNova.API.Common.Constants;
using VocaNova.API.Infrastructure.Persistence;
using VocaNova.API.Infrastructure.Persistence.Entities;

namespace VocaNova.API.Features.Lists.DAL.Mappings;

internal static class ListPersistenceMappings
{
    public static readonly Expression<Func<UserList, UserListSummary>> ToUserListSummary =
        list => new UserListSummary(
            list.ListId,
            list.ListName,
            list.UserListWords.Count(listWord => listWord.Word.Status == UserStatus.Active),
            list.CreatedAt);

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
}
