using System;
using System.Reflection;
using PCSC;
using System.Linq;

namespace PCSC_API_Test;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("PCSC 5.0 API Exploration");
        Console.WriteLine("=========================");
        
        // Get all types from PCSC assembly
        var pcscAssembly = typeof(SCardScope).Assembly;
        Console.WriteLine($"PCSC Assembly: {pcscAssembly.FullName}");
        
        // Look for ICardReader and its methods
        var icardReaderType = typeof(ICardReader);
        Console.WriteLine($"\nICardReader methods:");
        var methods = icardReaderType.GetMethods();
        foreach (var method in methods)
        {
            Console.WriteLine($"  {method.Name}({string.Join(", ", method.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}"))})");
        }
        
        // Look for ISCardContext and its methods
        var iscardContextType = typeof(ISCardContext);
        Console.WriteLine($"\nISCardContext methods:");
        var contextMethods = iscardContextType.GetMethods();
        foreach (var method in contextMethods)
        {
            Console.WriteLine($"  {method.Name}({string.Join(", ", method.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}"))})");
        }
        
        // Look for Transmit method specifically
        Console.WriteLine($"\nTransmit method details:");
        var transmitMethod = methods.FirstOrDefault(m => m.Name == "Transmit");
        if (transmitMethod != null)
        {
            Console.WriteLine($"  {transmitMethod}");
            var parameters = transmitMethod.GetParameters();
            for (int i = 0; i < parameters.Length; i++)
            {
                Console.WriteLine($"    Parameter {i}: {parameters[i].ParameterType.Name} {parameters[i].Name}");
            }
        }
    }
}
