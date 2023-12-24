using Microsoft.CodeAnalysis;
using System.Text;
using Microsoft.CodeAnalysis.Text;

namespace FirstOfficer.Generator.Syntax.Templates
{
    internal class Helpers
    {


        internal static void GetTemplate(SourceProductionContext context)
        {
            var classBlock = @"using System;
namespace FirstOfficer;

internal static class GeneratedHelpers
{
internal static DateTime RoundToNearestMillisecond(DateTime dateTime)
{
    long remainder = dateTime.Ticks % TimeSpan.TicksPerMillisecond;
    if (remainder >= TimeSpan.TicksPerMillisecond / 2)
    {
        return dateTime.AddTicks(TimeSpan.TicksPerMillisecond - remainder);
    }
    else
    {
        return dateTime.AddTicks(-remainder);
    }
}
}";

            context.AddSource("GeneratedHelpers.g",
                SourceText.From(classBlock, Encoding.UTF8));


        }
    }
}
