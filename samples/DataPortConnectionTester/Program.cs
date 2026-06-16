using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Spectre.Console;

namespace DataPortConnectionTester;

internal static class Program
{
    private static readonly Dictionary<string, Func<Task>> s_choices = new()
    {
    };

    internal static async Task Main()
    {
        var selection = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
            .Title("connection tests")
            .PageSize(11)
            .AddChoices(s_choices.Keys));

        try
        {
            await AnsiConsole.Status()
               .StartAsync("Trying to connect...", _ => s_choices[selection]());
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteException(ex);

            Console.ReadLine();
        }
    }
}
