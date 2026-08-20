using System.Linq.Expressions;
using VocaNova.API.Features.Notifications.BLL.Models;
using VocaNova.API.Infrastructure.Persistence.Entities;

namespace VocaNova.API.Features.Notifications.DAL.Mappings;

internal static class NotificationPersistenceMappings
{
    public static readonly Expression<Func<Word, DeletedWordReference>> ToDeletedWordReference =
        word => new DeletedWordReference(word.WordId, word.Word1, word.UpdatedAt);
}
