using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using PayItOff.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace PayItOff.MauiClient.Services
{
    public class SignalRService
    {
        private readonly HubConnection _connection;

        readonly string baseUrl = DeviceInfo.Platform == DevicePlatform.Android
               ? "http://192.168.0.89:5180/hubs/notifications"
               : "http://localhost:5180/hubs/notifications";

        public event Action? OnInvitationReceived;
        public event Action? OnExpenseUpdateReceived;
        public event Action<int>? OnUserKicked;
        public event Action? OnSettlementUpdateReceived;
        public event Action? OnSendGroupUpdateReceived;
        public event Action<int[]>? OnInitialPresenceReceived;
        public event Action<int, bool>? OnUserPresenceReceived;
        public event Action? OnFriendUpdateReceived;

        public bool IsDisconnected => _connection.State == HubConnectionState.Disconnected;

        public SignalRService()
        {
            _connection = new HubConnectionBuilder().WithUrl(baseUrl, options => options.AccessTokenProvider = () => SecureStorage.Default.GetAsync("jwt_token")).WithAutomaticReconnect().Build();
            _connection.On("ReceiveInvitation", () => OnInvitationReceived?.Invoke());
            _connection.On("ReceiveExpenseUpdate", () => OnExpenseUpdateReceived?.Invoke());
            _connection.On<int>("ReceiveKick", (groupId) => OnUserKicked?.Invoke(groupId));
            _connection.On("ReceiveSettlementUpdate", () => OnSettlementUpdateReceived?.Invoke());
            _connection.On("ReceiveGroupUpdate", () => OnSendGroupUpdateReceived?.Invoke());
            _connection.On<int, bool>("ReceiveUserPresence", (userId, isOnline) => OnUserPresenceReceived?.Invoke(userId, isOnline));
            _connection.On<int[]>("ReceiveInitialPresence", (userIds) => OnInitialPresenceReceived?.Invoke(userIds));
            _connection.On("ReceiveFriendUpdate", () => OnFriendUpdateReceived?.Invoke());
        }

        public async Task StartAsync()
        {
            try
            {
                await _connection.StartAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SIGNAL-R ERROR]: {ex.Message}");
                MainThread.BeginInvokeOnMainThread(() => Application.Current?.Windows[0].Page?.DisplayAlertAsync("Błąd SignalR", ex.Message, "OK"));
            }
        }

        public async Task JoinGroupAsync(int groupId)
        {
            if (IsDisconnected)
            {
                return;
            }

            await _connection.InvokeAsync("JoinGroup", groupId.ToString());
        }

        public async Task LeaveGroupAsync(int groupId)
        {
            if (IsDisconnected)
            {
                return;
            }

            await _connection.InvokeAsync("LeaveGroup", groupId.ToString());
        }
    }
}
