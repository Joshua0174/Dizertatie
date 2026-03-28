using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace PresentationLayer.Hubs
{
    [Authorize] // Ne asigurăm că doar cei logați corect prin cookie ajung aici
    public class NotificationHub : Hub
    {
        // Această metodă se rulează automat în secunda în care React-ul se conectează
        public override async Task OnConnectedAsync()
        {
            // Extragem ID-ul direct din token-ul validat de server (valabil pt orice tip de user)
            var userId = Context.User?.FindFirst("Id")?.Value;

            if (!string.IsNullOrEmpty(userId))
            {
                // Îl băgăm în camera lui privată, FORȚÂND litere mici pentru consistență absolută
                await Groups.AddToGroupAsync(Context.ConnectionId, userId.ToLowerInvariant());
            }

            await base.OnConnectedAsync();
        }
    }
}