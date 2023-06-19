using System;
using System.IO;
using LaserTagBox.Model.Body;
using LaserTagBox.Model.Mind;
using LaserTagBox.Model.Mind.Examples;
using LaserTagBox.Model.Spots;
using Mars.Components.Starter;
using Mars.Interfaces.Model;

namespace LaserTagBox;

internal static class Program
{
    private static void Main()
    {
        var description = new ModelDescription();
        description.AddLayer<PlayerMindLayer>();
        description.AddLayer<PlayerBodyLayer>();
        
        //Hill, Ditch und Barrier sind Entities. 
        description.AddAgent<Hill, PlayerBodyLayer>();
        description.AddAgent<Ditch, PlayerBodyLayer>();
        description.AddAgent<Barrier, PlayerBodyLayer>();
        description.AddAgent<PlayerBody, PlayerBodyLayer>();
        
        //description.AddAgent<Example1, PlayerMindLayer>();
        description.AddAgent<Example2, PlayerMindLayer>();
        description.AddAgent<Example3, PlayerMindLayer>();
        
        description.AddAgent<LearningBasedMindTest, PlayerMindLayer>();

        // USER: Add agents here
        //description.AddAgent<LearningBasedMind, PlayerMindLayer>();
        
        
        // USER: Specify JSON configuration file here
        var file = File.ReadAllText("config_3.json");
        Console.WriteLine(file);
        
        var config = SimulationConfig.Deserialize(file);

        Console.WriteLine(config);
        
        var starter = SimulationStarter.Start(description, config);
        var handle = starter.Run();
        Console.WriteLine("Successfully executed iterations: " + handle.Iterations);
        starter.Dispose();
    }
}