using VocaNova.API.Features.SuperAdmin.BLL.Models;
using VocaNova.API.Features.SuperAdmin.Contracts.Requests;
using VocaNova.API.Features.SuperAdmin.Contracts.Responses;

namespace VocaNova.API.Features.SuperAdmin.Mappings;

public static class SuperAdminMappingProfile
{
    public static AdminAccountResponse ToResponse(this AdminAccountModel model) =>
        new(model.AdminId, model.FullName, model.Email, model.Phone, model.Role, model.Status, model.CreatedAt, model.UpdatedAt, model.LastLoginAt);

    public static RoleResponse ToResponse(this RoleModel model) =>
        new(model.RoleId, model.RoleName);

    public static RoleUserResponse ToResponse(this RoleUserModel model) =>
        new(model.UserId, model.DisplayName, model.Email, model.Phone, model.Status);

    public static AdminAccountQuery ToModel(this AdminAccountQueryRequest request) =>
        new(request.Page, request.Limit, request.Status, request.Search, request.IncludeDeleted, request.SortBy, request.SortDirection);

    public static RoleQuery ToModel(this RoleQueryRequest request) =>
        new(request.Page, request.Limit, request.Search, request.Type, request.SortBy, request.SortDirection);

    public static CreateAdminAccountModel ToModel(this CreateAdminAccountRequest request) =>
        new(request.FullName, request.Email, request.Phone, request.Password, request.Status);

    public static UpdateAdminAccountModel ToModel(this UpdateAdminAccountRequest request) =>
        new(request.FullName, request.Email, request.Phone, request.Password, request.Status);

    public static SaveRoleModel ToModel(this SaveRoleRequest request) =>
        new(request.RoleName);
}
