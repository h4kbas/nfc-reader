using System;
using System.Reflection;
using PCSC;
using System.Linq;

namespace PCSC_Test;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("PCSC 5.0 Namespace Exploration");
        Console.WriteLine("===============================");
        
        // Get all types from PCSC assembly
        var pcscAssembly = typeof(SCardScope).Assembly;
        Console.WriteLine($"PCSC Assembly: {pcscAssembly.FullName}");
        
        // Get all namespaces
        var types = pcscAssembly.GetTypes();
        var namespaces = types.Select(t => t.Namespace).Distinct().OrderBy(n => n);
        
        Console.WriteLine("\nAvailable namespaces:");
        foreach (var ns in namespaces)
        {
            if (ns != null && ns.StartsWith("PCSC"))
            {
                Console.WriteLine($"  {ns}");
            }
        }
        
        // Show some key types
        Console.WriteLine("\nKey types:");
        var keyTypes = new[] { "SCardScope", "ICardReader", "SCardShareMode", "SCardProtocol", "ContextFactory", "IContextFactory" };
        foreach (var typeName in keyTypes)
        {
            var type = types.FirstOrDefault(t => t.Name == typeName);
            if (type != null)
            {
                Console.WriteLine($"  {typeName}: {type.FullName}");
            }
        }
        
        // Look for context-related types
        Console.WriteLine("\nContext-related types:");
        var contextTypes = types.Where(t => t.Name.Contains("Context") || t.Name.Contains("Factory")).OrderBy(t => t.Name);
        foreach (var type in contextTypes)
        {
            Console.WriteLine($"  {type.Name}: {type.FullName}");
        }
        
        // Look for monitoring types
        Console.WriteLine("\nMonitoring types:");
        var monitoringTypes = types.Where(t => t.Namespace == "PCSC.Monitoring").OrderBy(t => t.Name);
        foreach (var type in monitoringTypes)
        {
            Console.WriteLine($"  {type.Name}: {type.FullName}");
        }
    }
}
