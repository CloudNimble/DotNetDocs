using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Mintlify.Core;
using Mintlify.Core.Models;

public class TestDebug
{
    public static void Main()
    {
        var manager = new DocsJsonManager();
        
        var config1 = new DocsJsonConfig
        {
            Name = "Test",
            Navigation = new NavigationConfig
            {
                Pages = new List<object> 
                { 
                    new GroupConfig { Group = null!, Pages = new List<object> { "page1" } },
                    new GroupConfig { Group = "", Pages = new List<object> { "page2" } },
                    new GroupConfig { Group = "Valid Group", Pages = new List<object> { "page3" } }
                }
            }
        };
        manager.Load(JsonSerializer.Serialize(config1, MintlifyConstants.JsonSerializerOptions));
        
        Console.WriteLine($"After loading config1, Pages count: {manager.Configuration!.Navigation!.Pages!.Count}");
        foreach (var page in manager.Configuration!.Navigation!.Pages!)
        {
            if (page is GroupConfig g)
            {
                Console.WriteLine($"  Group: '{g.Group}', Pages: {string.Join(", ", g.Pages ?? new List<object>())}");
            }
        }

        var config2 = new DocsJsonConfig
        {
            Navigation = new NavigationConfig
            {
                Pages = new List<object> 
                { 
                    new GroupConfig { Group = null!, Pages = new List<object> { "page4" } },
                    new GroupConfig { Group = "Valid Group", Pages = new List<object> { "page5" } }
                }
            }
        };

        manager.Merge(config2);
        
        Console.WriteLine($"\nAfter merging config2, Pages count: {manager.Configuration!.Navigation!.Pages!.Count}");
        foreach (var page in manager.Configuration!.Navigation!.Pages!)
        {
            if (page is GroupConfig g)
            {
                Console.WriteLine($"  Group: '{g.Group}', Pages: {string.Join(", ", g.Pages ?? new List<object>())}");
            }
        }
        
        var nullGroups = manager.Configuration!.Navigation!.Pages!
            .OfType<GroupConfig>()
            .Where(g => g.Group == null)
            .ToList();
        Console.WriteLine($"\nNull groups count: {nullGroups.Count}");
    }
}