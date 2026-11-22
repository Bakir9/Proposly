using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace Proposly.API.Hubs
{
    [Authorize]
    public sealed class ChatHub : Hub
    {
        private static Guid UserId(HubCallerContext ctx) => 
            Guid.Parse(ctx.User!.FindFirstValue(ClaimTypes.NameIdentifier)!);

        public override async Task OnConnectedAsync()
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user:{UserId(Context)}");
            await base.OnConnectedAsync();  
        }

        public async Task Typing(Guid conversationId, Guid[] participantUserIds, string userName)
        {
            foreach (var uid in participantUserIds.Where(u => u != UserId(Context)))
                await Clients.Group($"user:{uid}")
                    .SendAsync("chat:typing", conversationId, userName);
        }
    }
}
