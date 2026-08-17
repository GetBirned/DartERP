using Microsoft.Extensions.DependencyInjection;

namespace DartERP.WinForms.Local;

/// <summary>
/// One of the Syncfusion WinForms packages ships its own
/// System.ServiceExtensions.GetRequiredService&lt;T&gt;(IServiceProvider),
/// same name and signature as Microsoft.Extensions.DependencyInjection's,
/// and since this project's global `using System;` (from ImplicitUsings)
/// puts it in scope everywhere, every .GetRequiredService&lt;T&gt;() call
/// became ambiguous the moment I added those packages. Renaming to
/// Resolve&lt;T&gt;() sidesteps the collision entirely instead of fully
/// qualifying every call site.
/// </summary>
internal static class ServiceProviderExtensions
{
    public static T Resolve<T>(this IServiceProvider provider) where T : notnull =>
        ServiceProviderServiceExtensions.GetRequiredService<T>(provider);
}
