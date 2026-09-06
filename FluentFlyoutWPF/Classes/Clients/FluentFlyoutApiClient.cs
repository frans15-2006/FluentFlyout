// Copyright (c) 2024-2026 The FluentFlyout Authors
// SPDX-License-Identifier: GPL-3.0-or-later

using FluentFlyout.Classes.Settings;
using System.Net.Http;
using System.Net.Http.Json;

namespace FluentFlyoutWPF.Classes.Clients;

public sealed class FluentFlyoutApiClient
{
    private static readonly object _lock = new();
    private static readonly TimeSpan _timeout = TimeSpan.FromSeconds(2);
    private static readonly Uri _uri = new("https://fluentflyout.com/api/");
    private const int MaxConsecutiveTimeouts = 3;

    private static int _consecutiveTimeouts;
    private static HttpClient _client;

    static FluentFlyoutApiClient()
    {
        _client = CreateClient();
    }

    public static async Task<string> GetStringAsync(string endpoint)
    {
        using var request = CreateRequest(HttpMethod.Get, endpoint);
        try
        {
            using var response = await _client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadAsStringAsync();
            OnRequestSucceeded();
            return result;
        }
        catch (Exception ex) when (IsTimeout(ex))
        {
            OnRequestTimedOut();
            throw;
        }
    }

    public static async Task PostAsJsonAsync<T>(string endpoint, T content)
    {
        using var request = CreateRequest(HttpMethod.Post, endpoint);
        request.Content = JsonContent.Create(content);
        try
        {
            await _client.SendAsync(request);
            OnRequestSucceeded();

        }
        catch (Exception ex) when (IsTimeout(ex))
        {
            OnRequestTimedOut();
            throw;
        }
    }

    private static bool IsTimeout(Exception ex)
    {
        return ex is TaskCanceledException { InnerException: TimeoutException }
            || ex is TimeoutException;
    }

    private static void OnRequestSucceeded()
    {
        Interlocked.Exchange(ref _consecutiveTimeouts, 0);
    }

    private static void OnRequestTimedOut()
    {
        int count = Interlocked.Increment(ref _consecutiveTimeouts);
        if (count >= MaxConsecutiveTimeouts)
        {
            RenewClient();
        }
    }


    private static HttpClient CreateClient()
    {
        return new HttpClient
        {
            Timeout = _timeout,
            BaseAddress = _uri
        };
    }

    private static HttpRequestMessage CreateRequest(HttpMethod method, string endpoint)
    {
        var request = new HttpRequestMessage(method, endpoint);

        // Per-request User-Agent: mutating the shared DefaultRequestHeaders
        // raced with in-flight requests that enumerate the same header
        // collection and could throw or send a torn header value.
        string appVersion = SettingsManager.Current.LastKnownVersion;
        string normalizedVersion = string.IsNullOrWhiteSpace(appVersion) ? "unknown" : appVersion;
        request.Headers.TryAddWithoutValidation("User-Agent", $"FluentFlyout/{normalizedVersion}");

        return request;
    }

    private static void RenewClient()
    {
        lock (_lock)
        {
            if (Interlocked.CompareExchange(ref _consecutiveTimeouts, 0, MaxConsecutiveTimeouts) < MaxConsecutiveTimeouts)
                return;

            // Swap the reference only: disposing the old HttpClient would
            // ObjectDisposedException every in-flight request still holding
            // it. The replaced instance becomes unreachable and its sockets
            // are reclaimed by finalizer/handler cleanup.
            _client = CreateClient();
            Interlocked.Exchange(ref _consecutiveTimeouts, 0);
        }
    }
}