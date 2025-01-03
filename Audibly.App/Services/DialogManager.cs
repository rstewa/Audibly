// Author: rstewa · https://github.com/rstewa
// Created: 09/29/2024
// Updated: 10/17/2024

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Audibly.App.Services;

public static class DialogManager
{
    private static readonly SemaphoreSlim _oneAtATimeAsync = new(1, 1);

    internal static async Task<T> OneAtATimeAsync<T>(Func<Task<T>> show, TimeSpan? timeout, CancellationToken? token)
    {
        var to = timeout ?? TimeSpan.FromHours(1);
        var tk = token ?? new CancellationToken(false);
        if (!await _oneAtATimeAsync.WaitAsync(to, tk))
            throw new Exception($"{nameof(DialogManager)}.{nameof(OneAtATimeAsync)} has timed out.");
        try
        {
            return await show();
        }
        finally
        {
            _oneAtATimeAsync.Release();
        }
    }
}