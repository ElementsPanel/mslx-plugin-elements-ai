namespace MSLX.Plugin.ElementsAI.Services;

public static class InteractionWait
{
    // Waiting for the user has no model/tool timeout. Keep the HTTP stream alive until
    // an answer arrives, the user stops, or the client connection actually fails.
    public static async Task<T> WaitAsync<T>(Task<T> answer, Func<Task> heartbeat,
        CancellationToken cancellationToken, TimeSpan? heartbeatInterval = null)
    {
        using var timer = new PeriodicTimer(heartbeatInterval ?? TimeSpan.FromSeconds(15));
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (answer.IsCompleted) return await answer;
            var tick = timer.WaitForNextTickAsync(cancellationToken).AsTask();
            await Task.WhenAny(answer, tick);
            cancellationToken.ThrowIfCancellationRequested();
            if (answer.IsCompleted) return await answer;
            if (await tick) await heartbeat();
        }
    }
}
