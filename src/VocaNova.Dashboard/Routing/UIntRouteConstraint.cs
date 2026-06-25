using System.Globalization;
using Microsoft.AspNetCore.Routing;

namespace VocaNova.Dashboard.Routing;

// Constraint {id:uint} dùng trong các route dashboard (khớp với VocaNova.API).
public sealed class UIntRouteConstraint : IRouteConstraint
{
    public bool Match(
        HttpContext? httpContext,
        IRouter? route,
        string routeKey,
        RouteValueDictionary values,
        RouteDirection routeDirection)
    {
        if (!values.TryGetValue(routeKey, out var routeValue))
        {
            return false;
        }

        var value = Convert.ToString(routeValue, CultureInfo.InvariantCulture);
        return value is not null && uint.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out _);
    }
}
