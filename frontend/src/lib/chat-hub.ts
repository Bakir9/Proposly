import { HubConnection, HubConnectionBuilder, HubConnectionState, LogLevel } from '@microsoft/signalr'


const HUB_URL = `${import.meta.env.VITE_API_BASE_URL ?? ''}/hubs/chat`;

let connection: HubConnection | null = null;

export function getChatHub(): HubConnection {
    if (!connection) {
        connection = new HubConnectionBuilder()
            .withUrl(HUB_URL, {
                accessTokenFactory: () => localStorage.getItem('token') ?? '',
                withCredentials: false,
            })
            .withAutomaticReconnect()
            .configureLogging(LogLevel.Information)
            .build();
    }
    return connection;
}

export async function startChatHub() {
  const hub = getChatHub()
  if (hub.state === HubConnectionState.Disconnected) {
    try { await hub.start() } catch { /* polling still covers us; reconnect retries anyway */ }
  }
}

export async function stopChatHub() {
  if (connection) {
    const c = connection
    connection = null
    try { await c.stop() } catch { /* already gone */ }
  }
}
