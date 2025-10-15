using System;
using System.Reflection;
using PCSC;

namespace SCardState_Test;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("SCardReaderState Structure Exploration");
        Console.WriteLine("=====================================");
        
        // Get the SCardReaderState type
        var scardReaderStateType = typeof(SCardReaderState);
        Console.WriteLine($"SCardReaderState Type: {scardReaderStateType.FullName}");
        
        // Get all properties
        Console.WriteLine("\nSCardReaderState Properties:");
        var properties = scardReaderStateType.GetProperties();
        foreach (var property in properties)
        {
            Console.WriteLine($"  {property.PropertyType.Name} {property.Name} {{ get; set; }}");
        }
        
        // Get all fields
        Console.WriteLine("\nSCardReaderState Fields:");
        var fields = scardReaderStateType.GetFields();
        foreach (var field in fields)
        {
            Console.WriteLine($"  {field.FieldType.Name} {field.Name}");
        }
        
        // Get all methods
        Console.WriteLine("\nSCardReaderState Methods:");
        var methods = scardReaderStateType.GetMethods();
        foreach (var method in methods)
        {
            if (!method.Name.StartsWith("get_") && !method.Name.StartsWith("set_"))
            {
                Console.WriteLine($"  {method.ReturnType.Name} {method.Name}()");
            }
        }
    }
}
