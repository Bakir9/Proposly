using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Proposly.API.Services;
using System.Security.Claims;

namespace Proposly.API.Hubs
{
    [Authorize]
    public sealed class ChatHub(ChatPresenceTracker presence) : Hub
    {
        private static Guid UserId(HubCallerContext ctx) =>
            Guid.Parse(ctx.User!.FindFirstValue(ClaimTypes.NameIdentifier)!);

        private static Guid CompanyId(HubCallerContext ctx) =>
            Guid.Parse(ctx.User!.FindFirstValue("company_id")!);

        public override async Task OnConnectedAsync()
        {
            var userId = UserId(Context);
            var companyId = CompanyId(Context);

            await Groups.AddToGroupAsync(Context.ConnectionId, $"user:{userId}");
            // Presence events stay inside the tenant — the company group is their audience.
            await Groups.AddToGroupAsync(Context.ConnectionId, $"company:{companyId}");
            await presence.HandleConnectedAsync(userId, companyId);

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            presence.HandleDisconnected(UserId(Context), CompanyId(Context));
            await base.OnDisconnectedAsync(exception);
        }

        public async Task Typing(Guid conversationId, Guid[] participantUserIds, string userName)
        {
            foreach (var uid in participantUserIds.Where(u => u != UserId(Context)))
                await Clients.Group($"user:{uid}")
                    .SendAsync("chat:typing", conversationId, userName);
        }
    }
}
