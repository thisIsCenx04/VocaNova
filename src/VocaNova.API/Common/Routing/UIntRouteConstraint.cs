using System.Globalization;
using Microsoft.AspNetCore.Routing;

namespace VocaNova.API.Common.Routing;

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
