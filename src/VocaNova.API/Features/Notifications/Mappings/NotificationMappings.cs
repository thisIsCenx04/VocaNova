using VocaNova.API.Common.Models;
using VocaNova.API.Features.Notifications.BLL.Models;
using VocaNova.API.Common.Results;
using VocaNova.API.Features.Notifications.Contracts.Requests;
using VocaNova.API.Features.Notifications.Contracts.Responses;

namespace VocaNova.API.Features.Notifications.Mappings;

public static class NotificationMappings
{
    public static NotificationListQuery ToBusinessQuery(this NotificationListRequest request) =>
        new(request.Page, request.Limit);

    public static PagedResult<NotificationResponse> ToResponse(
        this PagedCollection<Notification> notifications) =>
        new(
            notifications.Items.Select(ToResponse).ToList(),
            notifications.Page,
            notifications.Limit,
            notifications.TotalItems);

    private static NotificationResponse ToResponse(Notification notification) =>
        new(
            notification.NotificationId,
            notification.Type,
            notification.Title,
            notification.Message,
            notification.ReferenceType,
            notification.ReferenceId,
            notification.IsRead,
            notification.CreatedAt,
            notification.ReadAt);
}
