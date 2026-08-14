using DartERP.Core.Models;

namespace DartERP.Application;

/// <summary>
/// Holds whoever's signed in for the life of the process. WinForms has no
/// per-request scope the way a web app does, so — same call as every other
/// service in this app's DI container — this is just a singleton that gets
/// mutated in place rather than something re-created per "request".
/// </summary>
public class CurrentUserContext
{
    public User? CurrentUser { get; private set; }

    public void SignIn(User user) => CurrentUser = user;

    public void SignOut() => CurrentUser = null;
}
