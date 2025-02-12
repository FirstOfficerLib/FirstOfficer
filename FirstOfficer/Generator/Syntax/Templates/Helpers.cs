using System.Text;
using Microsoft.CodeAnalysis;
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
internal static DateTime? RoundToNearestMillisecond(DateTime? dateTime)
{
    if(dateTime == null)
    {
        return null;
    }

    var dateTimeValue = dateTime.Value;

    long remainder = dateTimeValue.Ticks % TimeSpan.TicksPerMillisecond;
    if (remainder >= TimeSpan.TicksPerMillisecond / 2)
    {
        return dateTimeValue.AddTicks(TimeSpan.TicksPerMillisecond - remainder);
    }
    else
    {
        return dateTimeValue.AddTicks(-remainder);
    }
}
}";

            context.AddSource("GeneratedHelpers.g",
                SourceText.From(classBlock, Encoding.UTF8));


        }
    }
}
