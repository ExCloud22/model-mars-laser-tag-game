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
        //description.AddAgent<Example2, PlayerMindLayer>();
        description.AddAgent<Example1, PlayerMindLayer>();
        
        // USER: Add agents here
        //description.AddAgent<LearningBasedMind, PlayerMindLayer>();
        description.AddAgent<RuleBasedMind, PlayerMindLayer>();

        description.AddAgent<Example2, PlayerMindLayer>();
        
        // USER: Specify JSON configuration file here
        var file = File.ReadAllText("config_3.json");
        var config = SimulationConfig.Deserialize(file);

        var starter = SimulationStarter.Start(description, config);
        var handle = starter.Run();
        Console.WriteLine("Successfully executed iterations: " + handle.Iterations);
        starter.Dispose();
    }
}