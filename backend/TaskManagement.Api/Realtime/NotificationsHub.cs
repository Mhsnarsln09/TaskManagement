using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace TaskManagement.Api.Realtime;

[Authorize]
public sealed class NotificationsHub : Hub;
