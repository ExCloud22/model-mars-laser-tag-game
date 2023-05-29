using System;
using System.Collections.Generic;
using System.Linq;
using LaserTagBox.Model.Shared;
using Mars.Interfaces.Environments;
using ServiceStack;

namespace LaserTagBox.Model.Mind;

/**
 * Diese Implementierung des Playermind ist ein learning-based Implementierung die den Q-Learning Algorithmus
 * benutzt. Der Agent befindet sich zu jedem tick in einem state und und erhält je nach state-action Paar einen
 * positiven oder negativen reward, in diesem Fall die Gamepoint. 
 */
public class LearningBasedMind : AbstractPlayerMind
{
    private PlayerMindLayer _mindLayer;
    private QFunction _qLearningAlgo; 

    public override void Init(PlayerMindLayer mindLayer)
    {
        double alpha = 0.1;
        double gamma = 0.1;
        double epsilon = 1.0; 
        _mindLayer = mindLayer;
        _qLearningAlgo = new QFunction(gamma, alpha, epsilon);
    }

    public override void Tick()
    {
        
    }
}

public class QFunction
{
    #region Fields

    /**
     * Learning-Rate alpha.
     * alpha > 0 (nahe 1), neue Erfahrungen werden in die Q-Wertschätzung integriert.
     * alpha < 1 (nahe 0), Q-Wert schätzung ändert sich nur langsam.
     *
     * Am Anfang: alpha = 0.1
     */
    private double alpha;
    
    /**
     * Discount-Rate gamma.
     * Zu Beginn ist gamma klein und wird pro Iteration größer. Dadurch wird das lernen beschleunigt.
     *
     * Am Anfang: gamma = 0.1
     */
    private double gamma; 
    
    /**
     * Epsilon value for epsilon-greedy policy. Am Anfang auf den höchsten Wert 1.0 um das Spielfeld zu erforschen.
     * Im späteren Verlauf des Algorithmus wird epsilon reduziert.
     *
     * Am Anfang: epsilon = 1.0
     */
    private double epsilon;
    
    // Q-Tabelle als multidimensionales Array
    private QTable qTable; 
    
    //Die Anzahl der möglichen Aktionen, die ein Agent wählen kann.  
    private int numberOfActions;
    
    private Random random;
    
    #endregion
    
    /**
     * Konstruktur für ein QLearning Objekt. 
     */
    public QFunction(double gamma, double alpha, double epsilon)
    {
        qTable = new QTable();
        this.gamma = gamma;
        this.alpha = alpha;
        this.epsilon = epsilon;
    }

    public double GetValue(GameState state, int action)
    {
        double[] qValues = qTable.GetQValues(state);
        return qValues[action];
    }

    public int ChooseAction(GameState state)
    {
        double[] qValues = qTable.GetQValues(state);
        //wähle ein Aktion unter Verwendung der epsilon-greedy policy
        if (random.NextDouble() < epsilon)
        {   
            return random.Next(numberOfActions);
        }
        else
        {
            return Array.IndexOf(qValues, qValues.Max());
        }

    }

    public void UpdateQValue(GameState state, int actionIndex, double reward, GameState nextState)
    {
        double[] qValues = qTable.GetQValues(state);
        double maxQValue = qTable.GetMaxQValue(nextState);
        double currentQValue = qValues[actionIndex];
        
        double newQValue = (1 - alpha) * currentQValue + alpha * (reward + gamma * maxQValue);
        qTable.UpdateQValue(state, actionIndex, newQValue);
    }
}

public class QTable
{
    private Dictionary<GameState, double[]> qTable;
    private int actionCount;
    private int initialQValue;

    public QTable()
    {
        actionCount = 10;
        //qValues = new Dictionary<GameState, double[]>();
        InitializeQTable();
    }
    
    public void InitializeQTable()
    {
        qTable = new Dictionary<GameState, double[]>();

        /*
        foreach (var state in possibleStates)
        {
            double[] qValues = new double[actionCount];
            for (int i = 0; i < actionCount; i++)
            {
                qValues[i] = initialQValue; // Setzen Sie den Anfangswert für alle Aktionen in diesem Zustand
            }
            qTable.Add(state, qValues);
        }
    */
    }
    
    public void UpdateQValue(GameState state, int actionIndex, double newQValue)
    {
        if (!qTable.ContainsKey(state))
        {
            qTable[state] = new double[actionCount];
        }
        qTable[state][actionIndex] = newQValue;
    }
    
    public double[] GetQValues(GameState state)
    {
        if (qTable.TryGetValue(state, out double[] values))
        {
            return values;
        }
        else
        {
            values = new double[actionCount];
            qTable[state] = values;
            return values;
        }
    }

    public double GetMaxQValue(GameState state)
    {
        double[] qValues = GetQValues(state);
        return qValues.Max();
    }
}

public class GameState
{
    public Position AgentPosition { get; set; }
    public Stance AgentStance { get; set; }
    public Dictionary<Position, bool> ExploredHills { get; set; }
    public Dictionary<Position, bool> ExploredBarriers { get; set; }
    public Dictionary<Position, bool> ExploredDitches { get; set; }
    public Dictionary<Guid, EnemySnapshot> Enemies { get; set; }
    public Dictionary<Guid, FriendSnapshot> TeamMates { get; set; }
}


 

