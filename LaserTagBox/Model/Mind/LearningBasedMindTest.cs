using System;
using System.Collections.Generic;
using System.Linq;
using LaserTagBox.Model.Shared;
using Mars.Common.Core.Random;
using Mars.Interfaces.Environments;
using static TorchSharp.torch;
using static TorchSharp.torch.nn;

// ReSharper disable All
namespace LaserTagBox.Model.Mind;

public class LearningBasedMindTest : AbstractPlayerMind
{
    #region Fields
    //diese Fields werden nicht benutzt
    //private Position _goal;
    //private List<PlayerBody> teammates;
    //private bool _isAssistantDead = false;
    //private bool _isAssistantLow = false;

    private PlayerMindLayer _mindLayer;
    //Die unique IDs der einzelnen Agenten-Rollen
    private Guid _shooter;
    private Guid _assistant;
    private Guid _scouter;
    private Position _enemyPosition;
    private Position _shooterPosition;
    
    //Hier drinn werden alle Informationen über einen enemy gespeichert
    private EnemySnapshot _enemy;
    //Hier drinn werden alle Informationen über eine Menge an enemys gespeichert
    private List<EnemySnapshot> _enemies;
    private List<Position> hills;
    private List<Position> ditches;
    //Die Rolle des Agents, also Shooter, Scouter oder Assistant
    private Role _role;
    private bool _goForwardShooter = false;
    private bool _goForwardAssistant = false;
    private bool _isScouterDead = false;
    private bool _isShooterDead = false;
    private bool _isScouterLow = false;
    private bool _isShooterLow = false;
    //Tracked ob irgendein Agent aus dem eigenen Team auf einem hill befindet
    private bool _aiOnTheHills = false;
    //Zählt wie oft sich pro Tick in einer Simulation ein Agent aus dem eigenen Team auf einem hill befindet 
    private int _tickOnTheHills;
    
    //Learning
    private int _tickCount = 0;
    private int _nGames = 0;
    private int _epsilon = 0; // randomness
    private int _rounds = 80;
    private readonly double _lr = 0.001;
    private const double Gamma = 0.9; // discount rate
    private const int BatchSize = 1000;
    private int _reward = 0;
    private bool _gameOver = false;
    
    private Random _random;
    private Position _positionOld;
    private QTrainer _trainer;
    private Module<Tensor, Tensor> _model;
    private readonly FixedSizedQueue<object[]> _memory = new FixedSizedQueue<object[]>(100000);
   #endregion
    
    
    /**
     *  Im Lasertagteam hat jeder Agent eine spezielle Rolle.
     *  Diese Rollen werden durch das Enum Role vergeben
     */
    private enum Role
    {
        Shooter, Scouter, Assistant
    }

    #region init
    /**
     *  Hier findet die Iniatilisierung des Rules-Based-Agent statt.
     *  Jeder der drei Agents bekommt eine eigene Rolle aus dem enum Role und
     *  eine ID
     */
    public override void Init(PlayerMindLayer mindLayer)
    {
        _mindLayer = mindLayer;
        if (_shooter == Guid.Empty)
        {
            _shooter = ID;
            _role = Role.Shooter;
        } else if (_assistant == Guid.Empty)
        {
            _assistant = ID;
            _role = Role.Assistant;
        } else if (_scouter == Guid.Empty)
        {
            _scouter = ID;
            _role = Role.Scouter;
        }
    }
    #endregion
    
    #region tick
    public override void Tick()
    {
        var stateOld = GetState();
        var finalMove = GetAction(stateOld);
        _tickCount++;
        _positionOld = new Position(Body.Position.X, Body.Position.Y); // Save old position
        
        
        // Game over logic
        _reward = 0;
        _gameOver = false;
        
        //Agent nicht im Hill und Es gibt Hill in der Nähe
        if (stateOld[8] == 1 && stateOld[6] == 1)
        {
            Body.GoTo(hills[0]);
        }
        else if(_enemies.Count > 0)
        {
            Body.GoTo(_enemies[0].Position);
        }
        else
        {
            Console.Write(finalMove[0] + "  " + finalMove[1]);
            Position position = new Position(Body.Position.X + finalMove[0], Body.Position.Y + finalMove[1]);
            Body.GoTo(position);
        }
        
        //Gegner ist da
        if (stateOld[0] == 1)
        {
            var sichtbarkeit = 0;

            if (stateOld[3] == 1)
            {
                sichtbarkeit = 10;
            } else if (stateOld[4] == 1)
            {
                sichtbarkeit = 8;
            } else if (stateOld[5] == 1)
            {
                sichtbarkeit = 5;
            }

            if (Body.GetDistance(_enemies[0].Position) <= sichtbarkeit)
            {
                if (Body.Tag5(_enemies[0].Position))
                {
                    _reward += 10;
                }
            }
        }
        
        //Train
        var stateNew = GetState();
        // train short memory
        TrainShortMemory(stateOld, finalMove, _reward, stateNew, _gameOver);
        // Remember
        Remember(stateOld, finalMove, _reward, stateNew, _gameOver);

        // Game over
        if (_tickCount % 1800 == 0)
        {
            _nGames++;

            TrainLongMemory();
        }
        //wenn die Energy von dem Agent >= 30 ist, soll je nach Role des Agents eine aggressive Strategie verfolgt werden
        /*if (Body.Energy >= 30)
        {
            switch (_role)
            {
                case Role.Shooter:
                    DoAggressiveStrategyForShooter(); break;
                case Role.Scouter:
                    DoAggressiveStrategyForScouter(); break;
                case Role.Assistant:
                    DoAggressiveStrategyForAssistant(); break;
            }
        }
        else
        {
            //ist die Energy kleiner < 30, soll eine allgemeine defensive Strategie verfolgt werden
            switch (_role)
            {
                case Role.Shooter:
                    _isShooterLow = true; break;
                case Role.Assistant:
                    _isScouterLow = true; break;
                //der Wert von _isAssistantLow wird nicht verwendet somit braucht man auch diesen Case nicht  
                /*
                case Role.Scouter:
                    _isAssistantLow = true; break;
                #1#
            }
            DoDefensiveStrategy();
        }*/
    }
    #endregion
    
    #region Methods
    
    private void DoAggressiveStrategyForShooter()
    {
        if (!_isScouterDead && !_isScouterLow)
        {
            if (!_goForwardShooter)
            {
                RandomMove();
                _shooterPosition = Body.Position;
                return;
            }
            _goForwardShooter = false;
        }else
        {
            RandomMove();
        }
        if (!GoForShot())
        {
            RandomMove();
        }
    }
    
    private void DoAggressiveStrategyForScouter()
    {
        //es befindet sich noch kein Agent aus dem eigenen Team auf einem hill 
        if (!_aiOnTheHills)
        {
            //Suche nach hills in der Umgebung, speicher die Pos. von den hills in eine Liste
            var hills = Body.ExploreHills1();
            //wenn min. 1 hill in Sichtweite dann gehe zu dem hill 
            if (hills.Count > 0)
            {
                //gehe zum nähesten hill 
                Body.GoTo(hills.OrderBy(x => Body.GetDistance(x)).FirstOrDefault());
                if (Body.GetDistance(hills.OrderBy(x => Body.GetDistance(x)).FirstOrDefault()) == 0)
                {
                    _aiOnTheHills = true;
                    _tickOnTheHills = 0;
                }
                //Scouter ist nun auf einem hill, somit können entsprechende Aktionen ausgeführt werden
                ActionOnTheHills();
            } else
            {
                //kein hill befindet sich in Sichtweite, Agent soll sich random bewegen. 
                RandomMove();
            }
        }
        //es befindet sich schon ein Agent auf irgendeinem einem hill
        else 
        {
            ActionOnTheHills();
            _tickOnTheHills++;
            if (_tickOnTheHills == 7 || Body.WasTaggedLastTick)
            {
                RandomMove();
                _aiOnTheHills = false;
            }
        }
    }
    
    private void DoAggressiveStrategyForAssistant()
    {
        if (!_isScouterDead && !_isShooterDead && !_isScouterLow && !_isShooterLow)
        {
            if (!_goForwardAssistant)
            {
                Body.GoTo(_shooterPosition);
                return;
            }
            _goForwardAssistant = false;
            if (!GoForShot())
            {
                RandomMove();
            }
        } else if (_isScouterDead || _isScouterLow)
        {
            DoAggressiveStrategyForScouter();
        } else if (_isShooterDead || _isShooterLow) 
        {
            RandomMove();
            
            if (!GoForShot())
            {
                RandomMove();
            }
        }
    }
    
    private void DoDefensiveStrategy()
    {
        //aktuallisiere zunächst den Alive Zustand von der aktuellen Agent-Role 
        switch (_role)
        {
            case Role.Shooter:
                if (!Body.Alive)
                {
                    _isShooterDead = true;
                }
                break;
            case Role.Scouter:
                if (!Body.Alive)
                {
                    _isScouterDead = true;
                }
                break;
            //der Wert von _isAssistantDead wird nicht verwendet somit braucht man auch diesen Case nicht 
            /*
            case Role.Assistant:
                if (!Body.Alive)
                {
                    _isAssistantDead = true;
                }
                break;
            */
        }
        //erforsche das Spielfeld nach Barieren und Gräben
        var exploreBarriers1 = Body.ExploreBarriers1(); 
        var exploreDitches1 = Body.ExploreDitches1();
        //gibt es mind. 1 Barriere in dem Sichtfeld des Agenten, dann gehe dahin und versuche einen Shot
        if (exploreBarriers1 != null && exploreBarriers1.Any())
        {
            Body.GoTo(exploreBarriers1[0]);
            GoForShot();
        }//gibt es mind. 1 Graben in dem Sichtfeld des Agenten, dann gehe dahin und versuche einen Shot 
        else if (exploreDitches1 != null && exploreDitches1.Any())
        {
            Body.GoTo(exploreDitches1[0]);
            GoForShot();
        }
        else
        {
            RandomMove();
        }
        
    }

    private void TellShooter()
    {
        _goForwardShooter = true;
    }
    
    private void TellAssistant()
    {
        _goForwardAssistant = true;
    }
    
    /**
     *  Diese Methode wird ausgeführt wenn sich ein Agent aus dem eigenen Team auf einem hill befindet.
     *  Die Position auf dem hill wird genutzt um den Agenten der Rollen Assistant und Shooter Informationen über einen
     *  Enemy zu geben. 
     */
    private void ActionOnTheHills()
    {
        _enemies = Body.ExploreEnemies1();
        if (_enemies != null && _enemies.Any())
        {
            _enemy = _enemies.First();
            _enemyPosition = _enemy.Position.Copy();
            TellAssistant();
            TellShooter();
            bool successRateForShooting = CheckSuccessRateForShooting(_enemy);
            if (Body.GetDistance(_enemyPosition) <= 5 && successRateForShooting)
            {
                if (Body.Stance != Stance.Lying)
                {
                    Body.ChangeStance2(Stance.Lying);
                }
                if (Body.RemainingShots == 0)
                {
                    Body.Reload3();
                
                }
                Body.Tag5(_enemyPosition);
                MoveAgentAfterShooting();
            }
        }
    }
    
    /**
     *  Diese Methode bestimmt mit einer sehr einfachen logik die Erfolgsrate für ein shot-attempt. 
     */
    private bool CheckSuccessRateForShooting(EnemySnapshot enemy)
    {
        //True if the enemy is not lying
        return enemy.Stance != Stance.Lying;
    }
    
    /**
     *  Diese Methode dient dazu den Agenten nach einem shot-attempt an
     *  eine andere Position zu positionieren. 
     */
    private void MoveAgentAfterShooting()
    {
        var tmpPos = Position.CreatePosition(Body.Position.X + 1, Body.Position.Y);
        Body.GoTo(tmpPos);
    }

    /**
     *  Die Methode dient dazu einen Agenten an eine zufällige Position
     *  zuführen. 
     */
    private void RandomMove()
    {
        var x = RandomHelper.Random.Next(48);
        var y = RandomHelper.Random.Next(48);

        var goal = Position.CreatePosition(x, y);
        Body.ChangeStance2(Stance.Standing);
        Body.GoTo(goal);
    }
    
    /**
     *  Diese Methode dient dazu das ein Agent einen Gegner versucht zu taggen.
     *  Dabei wird mit einer Success-Rate gearbeitet wie erfolgreich der Tag-Versuch ist. 
     */
    private bool GoForShot()
    {
        _enemies = Body.ExploreEnemies1();
        //gibt es mind. 1 Gegner in der Umgebung des Agents
        if (_enemies != null && _enemies.Any())
        {
            _enemy = _enemies.First();
            _enemyPosition = _enemy.Position.Copy();
            bool successRateForShooting = CheckSuccessRateForShooting(_enemy);
            if (Body.GetDistance(_enemyPosition) <= 5 && successRateForShooting)
            {
                if (Body.Stance != Stance.Lying)
                {
                    //erhoeht die Wahrscheinlichkeit das der Gegner erfolgreich getagged wird 
                    Body.ChangeStance2(Stance.Lying);
                }
                if (Body.RemainingShots == 0)
                {
                    Body.Reload3();
                }
                Body.Tag5(_enemyPosition);
                MoveAgentAfterShooting();
                return true; 
            }else
            {   
                //Distanz zu dem Gegner ist zu groß oder die Success-Rate stimmte nicht um zu schießen 
                return false;
            }
        }else
        {
            //es gibt keine Gegner in der Umgebung des Agents, somit wird auch nicht geschossen
            return false;
        }
    }
    
    public void LoadModel(string filename)
    {
        _model = new LinearQNet(11, 256, 3);
        //_model.load(filename);
        _trainer = new QTrainer(_model, lr: _lr, gamma: Gamma);
    }
    
    private int[] GetState()
    {
        // State structure: { enemy there, low hp, high hp, standing, kneeling, lying, hills , ditch }
        // Indexes:           [0]            [1]     [2]     [3]       [4]       [5]    [6]      [7]        
        // State structure: { on ground, on hill, enemy standing, enemy kneeling, enemy lying}
        // Indexes:           [8]        [9]      [10]             [11]             [12]               
        var state = new int[13];
        //var index = _clockwiseDirections.IndexOf(_currentDirection);
        _enemies = Body.ExploreEnemies1();
        // Enemy there
        state[0] = _enemies.Count == 0 ? 0 : 1;

        // low hp
        state[1] = Body.Energy > 30 ? 0 : 1;
        
        //high hp
        state[2] = Body.Energy <= 30 ? 0 : 1;
        
        //standing
        state[3] = Body.Stance != Stance.Standing ? 0 : 1;
        
        //kneeling
        state[4] = Body.Stance != Stance.Kneeling ? 0 : 1;
        
        //lying
        state[5] = Body.Stance != Stance.Lying ? 0 : 1;
        
        //hills
        hills = Body.ExploreHills1();
        state[6] = hills.Count == 0 ? 0 : 1;
        
        //ditch
        ditches = Body.ExploreDitches1();
        state[7] = ditches.Count == 0 ? 0 : 1;
        
        //on groud
        state[8] = !_aiOnTheHills ? 1 : 0;
        
        //on hills
        state[9] = _aiOnTheHills ? 1 : 0;
        
        //enemy standing
        state[10] = _enemies[0].Stance != Stance.Standing ? 0 : 1;
        
        //enemy kneeling
        state[11] = _enemies[0].Stance != Stance.Kneeling ? 0 : 1;

        //enemy lying
        state[12] = _enemies[0].Stance != Stance.Lying ? 0 : 1;
        
        return state;
    }

    private int[] GetAction(int[] state)
    {
        // Random moves: tradeoff exploration / exploitation
        _epsilon = _rounds - _nGames;
        var finalMove = new int[2];
        int move;
        if (_random.Next(201) < _epsilon)
        {
            move = _random.Next(2);
            finalMove[move] = 1;
        }
        else
        {
            var state0 = tensor(state, dtype: float32);
            var prediction = _model.forward(state0);
            move = (int)argmax(prediction).item<long>();
            finalMove[move] = 1;
        }

        return finalMove;
    }
    
    private void Move(int[] action)
    {
        Position pos = new Position(action[0], action[1]);
        Body.GoTo(pos);
    }
    
    private void Remember(int[] state, int[] action, int reward, int[] nextState, bool done)
    {
        _memory.Enqueue(new[] { state.Clone(), action.Clone(), reward, nextState.Clone(), done });
    }
    
    private void TrainLongMemory()
    {
        Queue<object[]> miniSample;
        if (_memory.Count() > BatchSize)
        {
            var rand = new Random();
            miniSample = new Queue<object[]>(_memory.Queue.OrderBy(x => rand.Next()).Take(BatchSize));
        }
        else
        {
            miniSample = new Queue<object[]>(_memory.Queue);
        }

        foreach (var tuple in miniSample)
        {
            _trainer.TrainStep(
                (int[])tuple[0], (int[])tuple[1], (int)tuple[2], (int[])tuple[3], (bool)tuple[4]);
        }
    }
    
    private void TrainShortMemory(int[] state, int[] action, int reward, int[] nextState, bool done)
    {
        _trainer.TrainStep(state, action, reward, nextState, done);
    }

    #endregion
}